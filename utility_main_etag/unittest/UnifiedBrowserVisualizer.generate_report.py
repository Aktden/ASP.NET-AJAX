import os
import tempfile
from UnifiedBrowserVisualizer import UnifiedBrowserVisualizer

class TestReportGeneration(unittest.TestCase):
    def setUp(self):
        self.df = pd.DataFrame({
            'age': [25, 35, 45],
            'salary': [50000, 70000, 90000]
        })
        self.visualizer = UnifiedBrowserVisualizer('dummy_path')
        self.visualizer.data = self.df
        self.visualizer.add_histogram('age')

    def test_generate_report(self):
        report_path = self.visualizer.generate_report()
        self.assertTrue(os.path.exists(report_path))
        os.remove(report_path)

if __name__ == '__main__':
    unittest.main()

'''Эти тесты проверяют создание HTML-отчета с графиками .

Что тестируется:
Успешное создание HTML-файла отчета.
Наличие файла на диске.
Удаление временного файла после теста.
Зачем это нужно:
Убедиться, что отчет действительно создаётся, и нет проблем с путями или записью файлов.'''