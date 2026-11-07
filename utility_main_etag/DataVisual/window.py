from PySide6.QtWidgets import QMainWindow, QWidget, QVBoxLayout, QHBoxLayout, QPushButton, QLabel, QTextEdit, QScrollArea, QGridLayout, QGroupBox, QStatusBar, QProgressBar, QFileDialog, QFrame
from PySide6.QtGui import QFont, QTextCursor, QColor, QTextCharFormat
from PySide6.QtCore import Qt, Signal
from datetime import datetime
from widgets import EnhancedFilterWidget, CheckBoxWithStatus
from analysis_thread import AnalysisThread
from styles import TelegramStyle
from visualizer import UnifiedBrowserVisualizer
import os

class VisualizerWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("DataVisual")
        self.resize(1000, 800)
        self.current_file = None
        self.analysis_thread = None
        self.last_report_path = None
        TelegramStyle.apply(self)
        main_widget = QWidget()
        self.setCentralWidget(main_widget)
        self.layout = QVBoxLayout(main_widget)
        self.layout.setSpacing(10)
        self.layout.setContentsMargins(15, 15, 15, 15)
        self.header = QWidget()
        header_layout = QHBoxLayout(self.header)
        header_layout.setContentsMargins(0, 0, 0, 0)
        self.title_label = QLabel("DataVisual")
        self.title_label.setFont(QFont("Segoe UI", 18, QFont.Bold))
        self.title_label.setStyleSheet("color: #ffffff;")
        header_layout.addWidget(self.title_label, alignment=Qt.AlignLeft)
        header_layout.addStretch()
        self.layout.addWidget(self.header)
        self.file_panel = QWidget()
        file_panel_layout = QHBoxLayout(self.file_panel)
        file_panel_layout.setContentsMargins(0, 0, 0, 0)
        self.select_btn = QPushButton("Выбрать файл")
        self.select_btn.setFixedHeight(40)
        self.select_btn.setCursor(Qt.PointingHandCursor)
        self.clear_btn = QPushButton("×")
        self.clear_btn.setFixedSize(40, 40)
        self.clear_btn.setCursor(Qt.PointingHandCursor)
        self.file_label = QLabel("Файл не выбран")
        self.file_label.setStyleSheet("color: #8f98a0; font-size: 14px;")
        self.file_label.setWordWrap(True)
        file_panel_layout.addWidget(self.select_btn)
        file_panel_layout.addWidget(self.file_label, stretch=1)
        file_panel_layout.addWidget(self.clear_btn)
        self.layout.addWidget(self.file_panel)
        scroll = QScrollArea()
        scroll.setWidgetResizable(True)
        scroll.setFrameShape(QFrame.NoFrame)
        content = QWidget()
        content_layout = QVBoxLayout(content)
        content_layout.setSpacing(15)
        content_layout.setContentsMargins(5, 5, 5, 5)
        group1 = QGroupBox("Основные визуализации")
        group1.setFont(QFont("Segoe UI", 12, QFont.Bold))
        g1_layout = QVBoxLayout()
        self.checkboxes1 = {
            "Информация о данных": CheckBoxWithStatus("Информация о данных"),
            "Гистограммы": CheckBoxWithStatus("Гистограммы"),
            "Диаграммы размаха": CheckBoxWithStatus("Диаграммы размаха (Boxplot)"),
            "Диаграммы рассеивания": CheckBoxWithStatus("Диаграммы рассеивания")
        }
        for cb in self.checkboxes1.values():
            g1_layout.addWidget(cb)
        group1.setLayout(g1_layout)
        group2 = QGroupBox("Статистические графики")
        group2.setFont(QFont("Segoe UI", 12, QFont.Bold))
        g2_layout = QVBoxLayout()
        self.checkboxes2 = {
            "Матрица корреляций": CheckBoxWithStatus("Матрица корреляций"),
            "Линейные графики": CheckBoxWithStatus("Линейные графики"),
            "Все графики": CheckBoxWithStatus("Все графики")
        }
        for cb in self.checkboxes2.values():
            g2_layout.addWidget(cb)
        group2.setLayout(g2_layout)
        group3 = QGroupBox("Дополнительные визуализации")
        group3.setFont(QFont("Segoe UI", 12, QFont.Bold))
        g3_layout = QVBoxLayout()
        self.checkboxes3 = {
            "Столбчатые диаграммы": CheckBoxWithStatus("Столбчатые диаграммы"),
            "Круговые диаграммы": CheckBoxWithStatus("Круговые диаграммы"),
            "Диаграммы скрипки": CheckBoxWithStatus("Диаграммы скрипки (Violin)"),
            "Матрицы рассеивания": CheckBoxWithStatus("Матрицы рассеивания"),
            "3D графики": CheckBoxWithStatus("3D графики"),
            "Тепловые карты": CheckBoxWithStatus("Тепловые карты"),
            "Радарные диаграммы": CheckBoxWithStatus("Радарные диаграммы"),
            "Временные ряды": CheckBoxWithStatus("Временные ряды")
        }
        for cb in self.checkboxes3.values():
            g3_layout.addWidget(cb)
        group3.setLayout(g3_layout)
        grid = QGridLayout()
        grid.addWidget(group1, 0, 0)
        grid.addWidget(group2, 0, 1)
        grid.addWidget(group3, 1, 0, 1, 2)
        content_layout.addLayout(grid)
        content_layout.addStretch()
        scroll.setWidget(content)
        self.layout.addWidget(scroll)
        bottom_panel = QWidget()
        bottom_panel.setFixedHeight(60)
        bottom_layout = QHBoxLayout(bottom_panel)
        bottom_layout.setContentsMargins(0, 0, 0, 0)
        self.run_btn = QPushButton("Сгенерировать отчет")
        self.run_btn.setFixedHeight(40)
        self.run_btn.setCursor(Qt.PointingHandCursor)
        self.run_btn.setEnabled(False)  # Активируется после выбора файла данных
        bottom_layout.addWidget(self.run_btn)
        self.save_btn = QPushButton("Сохранить отчет")
        self.save_btn.setFixedHeight(40)
        self.save_btn.setCursor(Qt.PointingHandCursor)
        self.save_btn.setEnabled(False)
        bottom_layout.addWidget(self.save_btn)
        self.layout.addWidget(bottom_panel)
        log_label = QLabel("Лог выполнения:")
        log_label.setFont(QFont("Segoe UI", 12, QFont.Bold))
        self.layout.addWidget(log_label)
        self.log_output = QTextEdit()
        self.log_output.setReadOnly(True)
        self.log_output.setFixedHeight(120)
        self.log_output.setFont(QFont("Consolas", 10))
        self.log_output.setStyleSheet("background-color: #17212b; border: 1px solid #2b5278;")
        self.layout.addWidget(self.log_output)
        self.status_bar = QStatusBar()
        self.setStatusBar(self.status_bar)
        self.progress_bar = QProgressBar()
        self.progress_bar.setFixedWidth(200)
        self.progress_bar.setTextVisible(True)
        self.progress_bar.setRange(0, 100)
        self.status_bar.addPermanentWidget(self.progress_bar)
        self.select_btn.clicked.connect(self.select_file)
        self.clear_btn.clicked.connect(self.clear_file)
        self.run_btn.clicked.connect(self.run_analysis)
        self.save_btn.clicked.connect(self.save_report)
        self.update_checkbox_statuses()
        # В конструкторе VisualizerWindow
        self.filter_widget = EnhancedFilterWidget([])  # Инициализируем пустой список столбцов
        filter_group = QGroupBox("Фильтрация данных")
        filter_group.setFont(QFont("Segoe UI", 12, QFont.Bold))
        filter_layout = QVBoxLayout()
        filter_layout.addWidget(self.filter_widget)
        filter_group.setLayout(filter_layout)
        self.layout.insertWidget(3, filter_group)
    def update_checkbox_statuses(self):
        if not self.current_file:
            for group in [self.checkboxes1, self.checkboxes2, self.checkboxes3]:
                for cb in group.values():
                    cb.set_status("Требуется файл данных", "gray")
                    cb.checkbox.setEnabled(False)
            return
        try:
            visualizer = UnifiedBrowserVisualizer(self.current_file)
            numeric_cols = visualizer.data.select_dtypes(include=['number']).columns.tolist()
            category_cols = visualizer.data.select_dtypes(include=['category', 'object']).columns.tolist()
            time_cols = visualizer._detect_time_columns()
            self.filter_widget.column_combo.clear()
            self.filter_widget.column_combo.addItems(visualizer.data.columns.tolist())
            self.checkboxes1["Информация о данных"].set_status("Доступно", "green")
            self.checkboxes1["Информация о данных"].checkbox.setEnabled(True)
            self.checkboxes1["Гистограммы"].set_status(
                "Доступно" if numeric_cols else "Нет числовых данных",
                "green" if numeric_cols else "red")
            self.checkboxes1["Гистограммы"].checkbox.setEnabled(bool(numeric_cols))
            self.checkboxes1["Диаграммы размаха"].set_status(
                "Доступно" if numeric_cols else "Нет числовых данных",
                "green" if numeric_cols else "red")
            self.checkboxes1["Диаграммы размаха"].checkbox.setEnabled(bool(numeric_cols))
            self.checkboxes1["Диаграммы рассеивания"].set_status(
                "Доступно" if len(numeric_cols) >= 2 else f"Нужно {2 - len(numeric_cols)} числовых столбцов",
                "green" if len(numeric_cols) >= 2 else "red")
            self.checkboxes1["Диаграммы рассеивания"].checkbox.setEnabled(len(numeric_cols) >= 2)
            self.checkboxes2["Матрица корреляций"].set_status(
                "Доступно" if len(numeric_cols) >= 2 else f"Нужно {2 - len(numeric_cols)} числовых столбцов",
                "green" if len(numeric_cols) >= 2 else "red")
            self.checkboxes2["Матрица корреляций"].checkbox.setEnabled(len(numeric_cols) >= 2)
            self.checkboxes2["Линейные графики"].set_status(
                "Доступно" if numeric_cols else "Нет числовых данных",
                "green" if numeric_cols else "red")
            self.checkboxes2["Линейные графики"].checkbox.setEnabled(bool(numeric_cols))
            self.checkboxes2["Все графики"].set_status(
                "Доступно" if numeric_cols else "Нет числовых данных",
                "green" if numeric_cols else "red")
            self.checkboxes2["Все графики"].checkbox.setEnabled(bool(numeric_cols))
            self.checkboxes3["Столбчатые диаграммы"].set_status(
                "Доступно" if numeric_cols and category_cols else "Нужны числовые и категориальные данные",
                "green" if numeric_cols and category_cols else "red")
            self.checkboxes3["Столбчатые диаграммы"].checkbox.setEnabled(bool(numeric_cols and category_cols))
            self.checkboxes3["Круговые диаграммы"].set_status(
                "Доступно" if category_cols else "Нет категориальных данных",
                "green" if category_cols else "red")
            self.checkboxes3["Круговые диаграммы"].checkbox.setEnabled(bool(category_cols))
            self.checkboxes3["Диаграммы скрипки"].set_status(
                "Доступно" if numeric_cols and category_cols else "Нужны числовые и категориальные данные",
                "green" if numeric_cols and category_cols else "red")
            self.checkboxes3["Диаграммы скрипки"].checkbox.setEnabled(bool(numeric_cols and category_cols))
            self.checkboxes3["Матрицы рассеивания"].set_status(
                "Доступно" if len(numeric_cols) >= 2 else f"Нужно {2 - len(numeric_cols)} числовых столбцов",
                "green" if len(numeric_cols) >= 2 else "red")
            self.checkboxes3["Матрицы рассеивания"].checkbox.setEnabled(len(numeric_cols) >= 2)
            self.checkboxes3["3D графики"].set_status(
                "Доступно" if len(numeric_cols) >= 3 else f"Нужно {3 - len(numeric_cols)} числовых столбцов",
                "green" if len(numeric_cols) >= 3 else "red")
            self.checkboxes3["3D графики"].checkbox.setEnabled(len(numeric_cols) >= 3)
            self.checkboxes3["Тепловые карты"].set_status(
                "Доступно" if len(numeric_cols) >= 2 else f"Нужно {2 - len(numeric_cols)} числовых столбцов",
                "green" if len(numeric_cols) >= 2 else "red")
            self.checkboxes3["Тепловые карты"].checkbox.setEnabled(len(numeric_cols) >= 2)
            self.checkboxes3["Радарные диаграммы"].set_status(
                "Доступно" if numeric_cols and category_cols else "Нужны числовые и категориальные данные",
                "green" if numeric_cols and category_cols else "red")
            self.checkboxes3["Радарные диаграммы"].checkbox.setEnabled(bool(numeric_cols and category_cols))
            self.checkboxes3["Временные ряды"].set_status(
                "Доступно" if time_cols and numeric_cols else "Нужны время и числовые данные",
                "green" if time_cols and numeric_cols else "red")
            self.checkboxes3["Временные ряды"].checkbox.setEnabled(bool(time_cols and numeric_cols))
        except Exception as e:
            self.log_message(f"Ошибка при проверке данных: {str(e)}", "error")
            for group in [self.checkboxes1, self.checkboxes2, self.checkboxes3]:
                for cb in group.values():
                    cb.set_status("Ошибка данных", "red")
                    cb.checkbox.setEnabled(False)
    def select_file(self):
        file_path, _ = QFileDialog.getOpenFileName(
            self, "Выберите файл данных", "", "CSV файлы (*.csv);;Excel файлы (*.xlsx *.xls);;Все файлы (*)"
        )
        if file_path:
            self.current_file = file_path
            self.file_label.setText(os.path.basename(file_path))
            self.file_label.setToolTip(file_path)
            self.log_message(f"Выбран файл: {file_path}")
            self.update_checkbox_statuses()
            self.run_btn.setEnabled(True)
    def clear_file(self):
        self.current_file = None
        self.file_label.setText("Файл не выбран")
        self.file_label.setToolTip("")
        self.log_message("Файл сброшен")
        self.update_checkbox_statuses()
        self.run_btn.setEnabled(False)
        self.save_btn.setEnabled(False)
    def log_message(self, message, level="info"):
        colors = {
            "info": "#ffffff",
            "success": "#5dc252",
            "warning": "#f0a732",
            "error": "#eb5545"
        }
        timestamp = datetime.now().strftime("%H:%M:%S")
        formatted_message = f"[{timestamp}] {message}"
        format = QTextCharFormat()
        format.setForeground(QColor(colors.get(level, "#ffffff")))
        self.log_output.moveCursor(QTextCursor.End)
        self.log_output.setCurrentCharFormat(format)
        self.log_output.insertPlainText(formatted_message + "\n")
        self.log_output.moveCursor(QTextCursor.End)
        self.log_output.ensureCursorVisible()

    def run_analysis(self):
        if not self.current_file:
            self.log_message("Файл не выбран!", "error")
            return
        options = {
            "data_info": self.checkboxes1["Информация о данных"].checkbox.isChecked(),
            "histograms": self.checkboxes1["Гистограммы"].checkbox.isChecked(),
            "boxplot": self.checkboxes1["Диаграммы размаха"].checkbox.isChecked(),
            "scatter": self.checkboxes1["Диаграммы рассеивания"].checkbox.isChecked(),
            "correlation": self.checkboxes2["Матрица корреляций"].checkbox.isChecked(),
            "line_chart": self.checkboxes2["Линейные графики"].checkbox.isChecked(),
            "bar_chart": self.checkboxes3["Столбчатые диаграммы"].checkbox.isChecked(),
            "pie_chart": self.checkboxes3["Круговые диаграммы"].checkbox.isChecked(),
            "violin_plot": self.checkboxes3["Диаграммы скрипки"].checkbox.isChecked(),
            "scatter_matrix": self.checkboxes3["Матрицы рассеивания"].checkbox.isChecked(),
            "all_plots": self.checkboxes2["Все графики"].checkbox.isChecked(),
            "3d_plot": self.checkboxes3["3D графики"].checkbox.isChecked(),
            "heatmap": self.checkboxes3["Тепловые карты"].checkbox.isChecked(),
            "radar_chart": self.checkboxes3["Радарные диаграммы"].checkbox.isChecked(),
            "time_series": self.checkboxes3["Временные ряды"].checkbox.isChecked()
        }



        if not any(options.values()):
            self.log_message("Ошибка: Не выбрано ни одной опции визуализации!", "error")
            self.log_message("Пожалуйста, выберите хотя бы один тип визуализации во вкладках:", "warning")
            self.log_message("- Основные визуализации", "info")
            self.log_message("- Статистические графики", "info")
            self.log_message("- Дополнительные визуализации", "info")
            return
        if options["all_plots"]:
            for key in options:
                if key != "all_plots":
                    options[key] = True

        filter_condition = self.filter_widget.get_filter_condition()

        self.run_btn.setEnabled(False)
        self.select_btn.setEnabled(False)
        self.clear_btn.setEnabled(False)
        self.progress_bar.setValue(0)
        self.log_message("Начало анализа данных...", "info")

        self.analysis_thread = AnalysisThread(self.current_file, options, filter_condition)
        self.analysis_thread.update_progress.connect(self.progress_bar.setValue)
        self.analysis_thread.update_status.connect(lambda msg: self.status_bar.showMessage(msg))
        self.analysis_thread.analysis_complete.connect(self.on_analysis_complete)
        self.analysis_thread.error_occurred.connect(self.on_analysis_error)
        self.analysis_thread.start()

    def on_analysis_complete(self, report_path):
        self.last_report_path = report_path
        self.run_btn.setEnabled(True)
        self.select_btn.setEnabled(True)
        self.clear_btn.setEnabled(True)
        self.save_btn.setEnabled(True)
        self.log_message(f"Анализ завершен. Отчет сохранен во временный файл: {report_path}", "success")
        self.status_bar.showMessage("Анализ завершен", 5000)

    def on_analysis_error(self, error_msg):
        self.run_btn.setEnabled(True)
        self.select_btn.setEnabled(True)
        self.clear_btn.setEnabled(True)
        if "Нет данных для отчета" in error_msg:
            error_msg = ("Не удалось создать визуализации. Возможные причины:\n"
                         "1. Выбранные типы графиков не поддерживаются для ваших данных\n"
                         "2. В данных отсутствуют необходимые столбцы (числовые/категориальные)\n"
                         "3. Данные содержат слишком много пропущенных значений")
        self.log_message(f"Ошибка анализа: {error_msg}", "error")
        self.status_bar.showMessage("Ошибка анализа", 5000)
        self.progress_bar.setValue(0)
    def save_report(self):
        if not self.last_report_path:
            self.log_message("Нет отчета для сохранения", "error")
            return
        file_path, _ = QFileDialog.getSaveFileName(
            self,
            "Сохранить отчет",
            f"data_report_{datetime.now().strftime('%Y%m%d_%H%M%S')}.html",
            "HTML файлы (*.html);;Все файлы (*)"
        )
        if file_path:
            try:
                import shutil
                shutil.copy(self.last_report_path, file_path)
                self.log_message(f"Отчет сохранен: {file_path}", "success")
            except Exception as e:
                self.log_message(f"Ошибка при сохранении отчета: {str(e)}", "error")