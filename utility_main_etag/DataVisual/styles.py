class TelegramStyle:
    COLORS = {
        "background": "#18222d",
        "primary": "#2b5278",
        "secondary": "#17212b",
        "text": "#ffffff",
        "text_secondary": "#8f98a0",
        "accent": "#5288c1",
        "button": "#2b5278",
        "button_hover": "#3d6d99",
        "button_pressed": "#1e3c5f",
        "success": "#5dc252",
        "error": "#eb5545",
        "warning": "#f0a732",
    }

    @classmethod
    def apply(cls, widget):
        """Apply a valid Qt stylesheet to the main window and its children."""
        colors = cls.COLORS
        widget.setStyleSheet(
            f"""
            QWidget {{
                background-color: {colors['secondary']};
                color: {colors['text']};
                font-family: 'Segoe UI', Arial, sans-serif;
            }}

            QMainWindow {{
                background-color: {colors['background']};
            }}

            QPushButton {{
                background-color: {colors['button']};
                color: {colors['text']};
                border: none;
                border-radius: 4px;
                padding: 8px 16px;
                font-size: 14px;
            }}
            QPushButton:hover {{
                background-color: {colors['button_hover']};
            }}
            QPushButton:pressed {{
                background-color: {colors['button_pressed']};
            }}
            QPushButton:disabled {{
                background-color: #39434d;
                color: {colors['text_secondary']};
            }}

            QLabel {{
                color: {colors['text']};
                background-color: transparent;
            }}

            QTextEdit {{
                background-color: {colors['secondary']};
                border: 1px solid {colors['primary']};
                border-radius: 4px;
                color: {colors['text']};
                padding: 8px;
            }}

            QCheckBox {{
                color: {colors['text']};
                spacing: 10px;
                padding: 4px;
                font-size: 14px;
            }}
            QCheckBox::indicator {{
                width: 20px;
                height: 20px;
                background-color: {colors['background']};
                border: 2px solid {colors['text']};
                border-radius: 4px;
            }}
            QCheckBox::indicator:hover {{
                background-color: {colors['primary']};
                border: 2px solid {colors['button_hover']};
            }}
            QCheckBox::indicator:checked {{
                background-color: {colors['error']};
                border: 2px solid {colors['error']};
            }}
            QCheckBox:disabled {{
                color: {colors['text_secondary']};
            }}

            QGroupBox {{
                border: 1px solid {colors['primary']};
                border-radius: 6px;
                margin-top: 10px;
                padding-top: 15px;
                color: {colors['text']};
            }}
            QGroupBox::title {{
                subcontrol-origin: margin;
                left: 10px;
                padding: 0 3px;
            }}

            QScrollArea {{
                border: none;
                background-color: transparent;
            }}
            QScrollBar:vertical {{
                border: none;
                background: {colors['secondary']};
                width: 8px;
                margin: 0;
            }}
            QScrollBar::handle:vertical {{
                background: {colors['primary']};
                min-height: 20px;
                border-radius: 4px;
            }}
            QScrollBar::add-line:vertical,
            QScrollBar::sub-line:vertical {{
                background: none;
                border: none;
            }}

            QStatusBar {{
                background-color: {colors['secondary']};
                border-top: 1px solid {colors['primary']};
            }}
            QProgressBar {{
                border: 1px solid {colors['primary']};
                border-radius: 3px;
                text-align: center;
                background: {colors['background']};
            }}
            QProgressBar::chunk {{
                background-color: {colors['accent']};
                width: 10px;
            }}
            """
        )
