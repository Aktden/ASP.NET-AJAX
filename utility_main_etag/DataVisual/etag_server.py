"""Local HTTP API for reading and updating the latest generated HTML report.

The API demonstrates HTTP ETag validation without adding third-party
runtime dependencies. It binds to localhost by default.
"""

from __future__ import annotations

import hashlib
import json
import os
import re
import tempfile
from http import HTTPStatus
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from pathlib import Path
from threading import RLock, Thread
from typing import Callable, Optional, Union


ReportPath = Union[str, os.PathLike[str]]
ReportPathProvider = Callable[[], Optional[ReportPath]]
MAX_REPORT_SIZE = 50 * 1024 * 1024
_ETAG_TOKEN_RE = re.compile(r'(?:W/)?"[^\"]*"|\*')


def make_etag(content: bytes) -> str:
    """Return a strong ETag based on the exact representation bytes."""
    digest = hashlib.sha256(content).hexdigest()
    return f'"{digest}"'


def _parse_etag_header(value: str) -> list[str]:
    """Extract entity-tags and wildcard tokens from an HTTP header."""
    return _ETAG_TOKEN_RE.findall(value or "")


def if_none_match_matches(header_value: str, current_etag: str) -> bool:
    """If-None-Match uses weak comparison for GET/HEAD requests."""
    for token in _parse_etag_header(header_value):
        if token == "*":
            return True
        opaque_tag = token[2:] if token.startswith("W/") else token
        if opaque_tag == current_etag:
            return True
    return False


def if_match_matches(header_value: str, current_etag: str) -> bool:
    """If-Match uses strong comparison; weak entity-tags never match."""
    for token in _parse_etag_header(header_value):
        if token == "*":
            return True
        if not token.startswith("W/") and token == current_etag:
            return True
    return False


class ETagReportServer:
    """Serve the latest report file with conditional GET and optimistic PUT."""

    def __init__(
        self,
        report_path_provider: ReportPathProvider,
        host: str = "127.0.0.1",
        port: int = 8765,
    ) -> None:
        self._report_path_provider = report_path_provider
        self._lock = RLock()
        handler_class = self._build_handler()
        self._httpd = ThreadingHTTPServer((host, port), handler_class)
        self._httpd.daemon_threads = True
        self._thread: Optional[Thread] = None

    @property
    def address(self) -> tuple[str, int]:
        host, port = self._httpd.server_address[:2]
        return str(host), int(port)

    @property
    def url(self) -> str:
        host, port = self.address
        return f"http://{host}:{port}"

    def start(self) -> None:
        if self._thread and self._thread.is_alive():
            return
        self._thread = Thread(
            target=self._httpd.serve_forever,
            name="etag-report-http-server",
            daemon=True,
        )
        self._thread.start()

    def stop(self) -> None:
        if self._thread and self._thread.is_alive():
            self._httpd.shutdown()
            self._thread.join(timeout=3)
        self._httpd.server_close()

    def _get_report_path(self) -> Optional[Path]:
        raw_path = self._report_path_provider()
        if raw_path is None:
            return None
        return Path(raw_path)

    def _build_handler(self):
        owner = self

        class ReportRequestHandler(BaseHTTPRequestHandler):
            protocol_version = "HTTP/1.1"
            server_version = "UtilityMainETag/1.0"

            def do_GET(self) -> None:
                if self.path != "/api/report":
                    self._send_json(HTTPStatus.NOT_FOUND, {"error": "Endpoint not found"})
                    return
                self._serve_report(include_body=True)

            def do_HEAD(self) -> None:
                if self.path != "/api/report":
                    self._send_json(HTTPStatus.NOT_FOUND, {"error": "Endpoint not found"}, head_only=True)
                    return
                self._serve_report(include_body=False)

            def do_PUT(self) -> None:
                if self.path != "/api/report":
                    self._send_json(HTTPStatus.NOT_FOUND, {"error": "Endpoint not found"})
                    return

                content_length = self.headers.get("Content-Length")
                if content_length is None:
                    self._send_json(HTTPStatus.LENGTH_REQUIRED, {"error": "Content-Length header is required"})
                    return
                try:
                    body_length = int(content_length)
                except ValueError:
                    self._send_json(HTTPStatus.BAD_REQUEST, {"error": "Invalid Content-Length"})
                    return
                if body_length < 0:
                    self._send_json(HTTPStatus.BAD_REQUEST, {"error": "Invalid Content-Length"})
                    return
                if body_length > MAX_REPORT_SIZE:
                    self._send_json(HTTPStatus.REQUEST_ENTITY_TOO_LARGE, {"error": "Report is too large"})
                    return

                body = self.rfile.read(body_length)
                if len(body) != body_length:
                    self._send_json(HTTPStatus.BAD_REQUEST, {"error": "Incomplete request body"})
                    return

                try:
                    with owner._lock:
                        report_path = owner._get_report_path()
                        if report_path is None or not report_path.is_file():
                            self._send_json(HTTPStatus.NOT_FOUND, {"error": "Report has not been generated yet"})
                            return

                        current_content = report_path.read_bytes()
                        current_etag = make_etag(current_content)
                        client_etag = self.headers.get("If-Match", "").strip()

                        if not client_etag:
                            self._send_json(
                                HTTPStatus.PRECONDITION_REQUIRED,
                                {
                                    "error": "If-Match header is required",
                                    "currentETag": current_etag,
                                },
                                etag=current_etag,
                            )
                            return

                        if not if_match_matches(client_etag, current_etag):
                            self._send_json(
                                HTTPStatus.PRECONDITION_FAILED,
                                {
                                    "error": "Report was modified by another client",
                                    "currentETag": current_etag,
                                    "yourETag": client_etag,
                                },
                                etag=current_etag,
                            )
                            return

                        new_etag = self._replace_report_atomically(report_path, body)

                    self._send_json(
                        HTTPStatus.OK,
                        {"message": "Report updated", "etag": new_etag},
                        etag=new_etag,
                    )
                except OSError as exc:
                    self._send_json(HTTPStatus.INTERNAL_SERVER_ERROR, {"error": str(exc)})

            def _serve_report(self, include_body: bool) -> None:
                try:
                    with owner._lock:
                        report_path = owner._get_report_path()
                        if report_path is None or not report_path.is_file():
                            self._send_json(
                                HTTPStatus.NOT_FOUND,
                                {"error": "Report has not been generated yet"},
                                head_only=not include_body,
                            )
                            return
                        content = report_path.read_bytes()
                except OSError as exc:
                    self._send_json(
                        HTTPStatus.INTERNAL_SERVER_ERROR,
                        {"error": str(exc)},
                        head_only=not include_body,
                    )
                    return

                etag = make_etag(content)
                if if_none_match_matches(self.headers.get("If-None-Match", ""), etag):
                    self.send_response(HTTPStatus.NOT_MODIFIED)
                    self.send_header("ETag", etag)
                    self.send_header("Cache-Control", "no-cache")
                    self.send_header("Content-Length", "0")
                    self.end_headers()
                    return

                self.send_response(HTTPStatus.OK)
                self.send_header("Content-Type", "text/html; charset=utf-8")
                self.send_header("Content-Length", str(len(content)))
                self.send_header("ETag", etag)
                self.send_header("Cache-Control", "no-cache")
                self.end_headers()
                if include_body:
                    self.wfile.write(content)

            def _send_json(
                self,
                status: HTTPStatus,
                payload: dict,
                etag: Optional[str] = None,
                head_only: bool = False,
            ) -> None:
                body = json.dumps(payload, ensure_ascii=False).encode("utf-8")
                self.send_response(status)
                self.send_header("Content-Type", "application/json; charset=utf-8")
                self.send_header("Content-Length", str(len(body)))
                if etag:
                    self.send_header("ETag", etag)
                self.end_headers()
                if not head_only:
                    self.wfile.write(body)

            @staticmethod
            def _replace_report_atomically(report_path: Path, content: bytes) -> str:
                report_path.parent.mkdir(parents=True, exist_ok=True)
                temp_name: Optional[str] = None
                try:
                    with tempfile.NamedTemporaryFile(
                        mode="wb",
                        dir=report_path.parent,
                        prefix=f".{report_path.name}.",
                        suffix=".tmp",
                        delete=False,
                    ) as temp_file:
                        temp_name = temp_file.name
                        temp_file.write(content)
                        temp_file.flush()
                        os.fsync(temp_file.fileno())
                    os.replace(temp_name, report_path)
                finally:
                    if temp_name and os.path.exists(temp_name):
                        os.unlink(temp_name)
                return make_etag(content)

            def log_message(self, format_string: str, *args) -> None:
                # Keep the local API's requests visible in the console.
                super().log_message(format_string, *args)

        return ReportRequestHandler
