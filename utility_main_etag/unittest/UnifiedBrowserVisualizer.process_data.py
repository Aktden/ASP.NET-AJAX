import pandas as pd
from UnifiedBrowserVisualizer import UnifiedBrowserVisualizer

class TestDataProcessing(unittest.TestCase):
    def setUp(self):
        self.df = pd.DataFrame({
            'age': [25, 35, 45],
            'salary': [50000, 70000, 90000],
            'department': ['IT', 'HR', 'IT']
        })
        self.visualizer = UnifiedBrowserVisualizer('dummy_path')
        self.visualizer.data = self.df

    def test_histogram(self):
        self.visualizer.add_histogram('age')
        self.assertEqual(len(self.visualizer.figures), 1)

    def test_boxplot(self):
        self.visualizer.add_boxplot('salary')
        self.assertEqual(len(self.visualizer.figures), 1)

    def test_scatter(self):
        self.visualizer.add_scatter('age', 'salary')
        self.assertEqual(len(self.visualizer.figures), 1)

    def test_correlation_matrix(self):
        self.visualizer.add_correlation_matrix()
        self.assertEqual(len(self.visualizer.figures), 1)

if __name__ == '__main__':
    unittest.main()

'''Эти тесты проверяют функции визуализации , такие как гистограммы, диаграммы размаха, графики рассеивания и матрица корреляций.

Что тестируется:
Добавление разных типов графиков (histogram, boxplot, scatter, correlation matrix).
Увеличение счетчика графиков после добавления — проверка успешности выполнения функций.
Зачем это нужно:
Проверить, что функции построения графиков работают без ошибок и добавляют ожидаемые элементы в список figures.'''