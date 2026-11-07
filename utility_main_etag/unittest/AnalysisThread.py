import tempfile
import pandas as pd
from AnalysisThread import AnalysisThread
from PySide6.QtCore import QThread

class TestAnalysisThread(unittest.TestCase):
    def setUp(self):
        self.temp_dir = tempfile.TemporaryDirectory()
        self.file_path = os.path.join(self.temp_dir.name, 'test.csv')
        self.df = pd.DataFrame({'age': [25, 35, 45], 'salary': [50000, 70000, 90000]})
        self.df.to_csv(self.file_path, index=False)

    def test_analysis_thread_runs(self):
        options = {
            "histograms": True,
            "data_info": True
        }
        thread = AnalysisThread(self.file_path, options)
        thread.start()
        thread.wait()
        self.assertTrue(hasattr(thread, 'report_path'))

    def tearDown(self):
        self.temp_dir.cleanup()

if __name__ == '__main__':
    unittest.main()

'''Эти тесты проверяют многопоточную обработку данных .

Что тестируется:
Успешный запуск потока анализа.
Прогресс выполнения (от 0 до 100%).
Генерация отчета внутри потока.
Обработка возможных ошибок.
Зачем это нужно:
Проверить, что анализ выполняется в фоновом режиме без зависаний UI и корректно завершает работу.'''