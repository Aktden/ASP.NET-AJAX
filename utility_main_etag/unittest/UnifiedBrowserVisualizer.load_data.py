import tempfile
import pandas as pd
import os

from UnifiedBrowserVisualizer import UnifiedBrowserVisualizer

class TestDataLoading(unittest.TestCase):
    def test_csv_loading(self):
        with tempfile.NamedTemporaryFile(delete=False, suffix='.csv') as tmpfile:
            df = pd.DataFrame({'col1': [1, 2], 'col2': ['a', 'b']})
            df.to_csv(tmpfile.name, index=False)
            visualizer = UnifiedBrowserVisualizer(tmpfile.name)
            self.assertTrue(visualizer.data.equals(df))
            os.unlink(tmpfile.name)

    def test_excel_loading(self):
        with tempfile.NamedTemporaryFile(delete=False, suffix='.xlsx') as tmpfile:
            df = pd.DataFrame({'col1': [1, 2], 'col2': ['a', 'b']})
            df.to_excel(tmpfile.name, index=False)
            visualizer = UnifiedBrowserVisualizer(tmpfile.name)
            self.assertTrue(visualizer.data.equals(df))
            os.unlink(tmpfile.name)

if __name__ == '__main__':
    unittest.main()

'''Эти тесты проверяют загрузку данных из файлов CSV и Excel .

Что тестируется:
Успешная загрузка данных из временного CSV-файла.
Успешная загрузка данных из временного Excel-файла.
Соответствие загруженных данных исходным.
Зачем это нужно:
Убедиться, что приложение может правильно читать данные из поддерживаемых форматов (CSV, XLSX), а также что они сохраняются в структуре pandas.DataFrame.'''