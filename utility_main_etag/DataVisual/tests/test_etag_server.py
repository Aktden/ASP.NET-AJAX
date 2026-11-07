import tempfile
import unittest
from pathlib import Path
from urllib.error import HTTPError
from urllib.request import Request, urlopen

from etag_server import ETagReportServer


class ETagReportServerTests(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.report_path = Path(self.temp_dir.name) / "report.html"
        self.report_path.write_text("<h1>Initial report</h1>", encoding="utf-8")

        self.server = ETagReportServer(lambda: self.report_path, port=0)
        self.server.start()
        self.base_url = f"{self.server.url}/api/report"

    def tearDown(self):
        self.server.stop()
        self.temp_dir.cleanup()

    def request(self, method="GET", headers=None, data=None):
        request = Request(
            self.base_url,
            data=data,
            headers=headers or {},
            method=method,
        )
        try:
            response = urlopen(request, timeout=3)
        except HTTPError as error:
            return error.code, dict(error.headers.items()), error.read()
        with response:
            return response.status, dict(response.headers.items()), response.read()

    def test_get_returns_report_and_strong_etag(self):
        status, headers, body = self.request()

        self.assertEqual(status, 200)
        self.assertEqual(body, b"<h1>Initial report</h1>")
        self.assertRegex(headers["ETag"], r'^"[0-9a-f]{64}"$')

    def test_if_none_match_returns_304(self):
        _, headers, _ = self.request()
        status, response_headers, body = self.request(
            headers={"If-None-Match": headers["ETag"]}
        )

        self.assertEqual(status, 304)
        self.assertEqual(response_headers["ETag"], headers["ETag"])
        self.assertEqual(body, b"")

    def test_if_none_match_uses_weak_comparison(self):
        _, headers, _ = self.request()
        weak_etag = f"W/{headers['ETag']}"
        status, _, body = self.request(headers={"If-None-Match": weak_etag})

        self.assertEqual(status, 304)
        self.assertEqual(body, b"")

    def test_put_without_if_match_returns_428(self):
        status, _, body = self.request(
            method="PUT",
            headers={"Content-Type": "text/html"},
            data=b"<h1>Updated</h1>",
        )

        self.assertEqual(status, 428)
        self.assertIn(b"If-Match header is required", body)
        self.assertEqual(self.report_path.read_text(encoding="utf-8"), "<h1>Initial report</h1>")

    def test_second_client_with_old_etag_gets_412(self):
        _, headers, _ = self.request()
        client_a_etag = headers["ETag"]

        # Client A updates the report using the version it originally read.
        first_status, first_headers, _ = self.request(
            method="PUT",
            headers={"If-Match": client_a_etag},
            data=b"<h1>Update from client A</h1>",
        )
        self.assertEqual(first_status, 200)
        self.assertNotEqual(first_headers["ETag"], client_a_etag)

        # Client B still has the old ETag, so its update must be rejected.
        second_status, _, second_body = self.request(
            method="PUT",
            headers={"If-Match": client_a_etag},
            data=b"<h1>Update from client B</h1>",
        )

        self.assertEqual(second_status, 412)
        self.assertIn(b"Report was modified by another client", second_body)
        self.assertEqual(
            self.report_path.read_text(encoding="utf-8"),
            "<h1>Update from client A</h1>",
        )

    def test_put_with_weak_if_match_is_rejected(self):
        _, headers, _ = self.request()
        status, _, _ = self.request(
            method="PUT",
            headers={"If-Match": f"W/{headers['ETag']}"},
            data=b"<h1>Updated</h1>",
        )

        self.assertEqual(status, 412)

    def test_put_with_current_etag_updates_report_and_etag(self):
        _, headers, _ = self.request()
        old_etag = headers["ETag"]
        new_content = b"<h1>Updated report</h1>"
        status, response_headers, response_body = self.request(
            method="PUT",
            headers={
                "If-Match": old_etag,
                "Content-Type": "text/html; charset=utf-8",
            },
            data=new_content,
        )

        self.assertEqual(status, 200)
        self.assertNotEqual(response_headers["ETag"], old_etag)
        self.assertIn(b"Report updated", response_body)
        self.assertEqual(self.report_path.read_bytes(), new_content)

        get_status, get_headers, get_body = self.request()
        self.assertEqual(get_status, 200)
        self.assertEqual(get_headers["ETag"], response_headers["ETag"])
        self.assertEqual(get_body, new_content)

    def test_if_match_wildcard_accepts_existing_report(self):
        status, _, _ = self.request(
            method="PUT",
            headers={"If-Match": "*"},
            data=b"<h1>Wildcard update</h1>",
        )

        self.assertEqual(status, 200)


if __name__ == "__main__":
    unittest.main()
