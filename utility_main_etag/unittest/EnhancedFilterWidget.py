import unittest
from PySide6.QtWidgets import QApplication
from EnhancedFilterWidget import EnhancedFilterWidget

app = QApplication([])

class TestEnhancedFilterWidget(unittest.TestCase):
    def test_filter_condition(self):
        widget = EnhancedFilterWidget(["name", "age", "salary"])
        widget.column_combo.setCurrentText("age")
        widget.operator_combo.setCurrentText("Больше")
        widget.value_edit.setText("30")

        condition = widget.get_filter_condition()
        self.assertEqual(condition, "age > 30")

    def test_invalid_value(self):
        widget = EnhancedFilterWidget(["name", "age", "salary"])
        widget.column_combo.setCurrentText("age")
        widget.operator_combo.setCurrentText("Равно")
        widget.value_edit.setText("")
        condition = widget.get_filter_condition()
        self.assertIsNone(condition)

if __name__ == '__main__':
    unittest.main()

"""Что тестируется:
Формирование строки условия фильтрации , например: age > 30.
Проверка обработки пустого значения , чтобы не было ошибок.
Правильная обработка операторов (например, str.contains для поиска подстроки).
Зачем это нужно:
Убедиться, что пользовательский интерфейс правильно преобразует выбор пользователя в корректное выражение для фильтрации данных (pandas.query())."""