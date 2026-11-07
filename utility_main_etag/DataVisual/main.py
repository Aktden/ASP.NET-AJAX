import sys
from PySide6.QtWidgets import QApplication
from window import VisualizerWindow
from etag_server import ETagReportServer


def main():
    app = QApplication(sys.argv)
    app.setStyle("Fusion")

    window = VisualizerWindow()
    api_server = None

    try:
        # The report becomes available through the API after analysis finishes.
        api_server = ETagReportServer(lambda: window.last_report_path)
        api_server.start()
        window.log_message(
            f"ETag API запущен: {api_server.url}/api/report",
            "success",
        )
    except OSError as error:
        window.log_message(
            f"Не удалось запустить ETag API: {error}",
            "error",
        )

    window.show()

    try:
        exit_code = app.exec()
    finally:
        if api_server is not None:
            api_server.stop()

    sys.exit(exit_code)


if __name__ == "__main__":
    main()
