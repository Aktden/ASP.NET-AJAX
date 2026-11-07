from PySide6.QtCore import Qt
from PySide6.QtWidgets import (
    QWidget, QHBoxLayout, QComboBox, QLineEdit, QVBoxLayout, QCheckBox, QLabel
)


class EnhancedFilterWidget(QWidget):
    OPERATORS = [
        ("Равно", "=="),
        ("Не равно", "!="),
        ("Больше", ">"),
        ("Меньше", "<"),
        ("Больше или равно", ">="),
        ("Меньше или равно", "<="),
        ("Содержит", "str.contains"),
    ]

    def __init__(self, columns):
        super().__init__()
        layout = QHBoxLayout()

        self.column_combo = QComboBox()
        self.column_combo.addItems(columns)

        self.operator_combo = QComboBox()
        for label, _ in self.OPERATORS:
            self.operator_combo.addItem(label)

        self.value_edit = QLineEdit()
        self.value_edit.setPlaceholderText("Значение")

        layout.addWidget(self.column_combo)
        layout.addWidget(self.operator_combo)
        layout.addWidget(self.value_edit)
        self.setLayout(layout)

    def get_filter_condition(self):
        column = self.column_combo.currentText()
        op_label = self.operator_combo.currentText()
        value = self.value_edit.text().strip()

        if not column or not value:
            return None

        op = next((v for l, v in self.OPERATORS if l == op_label), None)
        if not op:
            return None

        if "contains" in op:
            return f'{column}.str.contains("{value}", case=False)'
        else:
            return f'{column} {op} "{value}"'
class CheckBoxWithStatus(QWidget):
    def __init__(self, label_text):
        super().__init__()
        self.label_text = label_text
        self.checkbox = QCheckBox(label_text)
        self.status_label = QLabel("")
        self.init_ui()
    def init_ui(self):
        layout = QVBoxLayout()
        layout.setContentsMargins(5, 0, 5, 0)
        layout.setSpacing(2)
        checkbox_layout = QHBoxLayout()
        checkbox_layout.addWidget(self.checkbox, alignment=Qt.AlignLeft)
        layout.addLayout(checkbox_layout)
        self.status_label.setStyleSheet("font-size: 12px; color: #8f98a0;")
        layout.addWidget(self.status_label)
        self.setLayout(layout)
    def set_status(self, text, color="gray"):
        colors = {
            "gray": "#8f98a0",
            "green": "#5dc252",
            "red": "#eb5545",
            "blue": "#5288c1"
        }
        self.status_label.setText(text)
        self.status_label.setStyleSheet(f"color: {colors.get(color, color)}; font-size: 12px;")