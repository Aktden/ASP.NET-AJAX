import pandas as pd
import plotly.graph_objects as go
import plotly.express as px
from datetime import datetime
import numpy as np
import tempfile
import webbrowser
from pathlib import Path
import io
import base64

try:
    import matplotlib.pyplot as plt
except ImportError:
    plt = None

class UnifiedBrowserVisualizer:
    def __init__(self, file_path):
        self.file_path = file_path
        self.data = None
        self.figures = []
        self.load_data()
    def load_data(self):
        path = Path(self.file_path)
        if not path.exists():
            raise FileNotFoundError(f"Файл '{self.file_path}' не найден")
        if path.suffix == '.csv':
            try:
                self.data = pd.read_csv(path, low_memory=False)
            except:
                self.data = pd.read_csv(path, header=None, low_memory=False)
        elif path.suffix in ['.xlsx', '.xls']:
            self.data = pd.read_excel(path)
        else:
            raise ValueError("Неподдерживаемый формат файла. Используйте CSV или Excel.")
        self.optimize_memory()
    def optimize_memory(self):
        if self.data is not None:
            for col in self.data.select_dtypes(include=['object']):
                if self.data[col].nunique() / len(self.data) < 0.5:
                    self.data[col] = self.data[col].astype('category')
            for col in self.data.select_dtypes(include=['int']):
                c_min = self.data[col].min()
                c_max = self.data[col].max()
                if c_min > np.iinfo(np.int8).min and c_max < np.iinfo(np.int8).max:
                    self.data[col] = self.data[col].astype(np.int8)
                elif c_min > np.iinfo(np.int16).min and c_max < np.iinfo(np.int16).max:
                    self.data[col] = self.data[col].astype(np.int16)
                elif c_min > np.iinfo(np.int32).min and c_max < np.iinfo(np.int32).max:
                    self.data[col] = self.data[col].astype(np.int32)
                elif c_min > np.iinfo(np.int64).min and c_max < np.iinfo(np.int64).max:
                    self.data[col] = self.data[col].astype(np.int64)
            for col in self.data.select_dtypes(include=['float']):
                self.data[col] = pd.to_numeric(self.data[col], downcast='float')
    def process_data(self, options):
        if options.get("data_info", False):
            self.add_data_info()
        numeric_cols = self.data.select_dtypes(include=['number']).columns.tolist()
        category_cols = self.data.select_dtypes(include=['category', 'object']).columns.tolist()
        time_cols = self._detect_time_columns()

        if options.get("histograms", False) and numeric_cols:
            for col in numeric_cols[:3]:
                self.add_histogram(col)
        if options.get("boxplot", False) and numeric_cols:
            for col in numeric_cols[:3]:
                self.add_boxplot(col)
        if options.get("scatter", False) and len(numeric_cols) >= 2:
            self.add_scatter(numeric_cols[0], numeric_cols[1])
        if options.get("correlation", False) and len(numeric_cols) >= 2:
            self.add_correlation_matrix()
        if options.get("line_chart", False) and numeric_cols:
            self.add_line_chart()
        if options.get("bar_chart", False) and numeric_cols and category_cols:
            self.add_bar_chart()
        if options.get("pie_chart", False) and category_cols:
            self.add_pie_chart()
        if options.get("violin_plot", False) and numeric_cols and category_cols:
            self.add_violin_plot()
        if options.get("scatter_matrix", False) and len(numeric_cols) >= 2:
            self.add_scatter_matrix()
        if options.get("3d_plot", False) and len(numeric_cols) >= 3:
            self.add_3d_plot()
        if options.get("heatmap", False) and len(numeric_cols) >= 2:
            self.add_heatmap()
        if options.get("radar_chart", False) and numeric_cols and category_cols:
            self.add_radar_chart()
        if options.get("time_series", False) and time_cols and numeric_cols:
            self.add_time_series(time_cols[0], numeric_cols[0])
    def _detect_time_columns(self):
        time_cols = []
        for col in self.data.columns:
            try:
                pd.to_datetime(self.data[col], errors='coerce')
                time_cols.append(col)
            except:
                pass
        return time_cols
    def add_data_info(self):
        try:
            info_df = pd.DataFrame({
                'Column': self.data.columns,
                'Type': self.data.dtypes.astype(str),
                'Missing': self.data.isna().sum(),
                'Unique': self.data.nunique()
            })
            numeric_stats = self.data.describe().transpose()
            info_df = info_df.join(numeric_stats, on='Column', how='left')
            info_df = info_df.sort_values(by=['Missing', 'Unique'], ascending=[False, True])
            fig = go.Figure(data=[go.Table(
                header=dict(values=list(info_df.columns), fill_color='paleturquoise', align='left'),
                cells=dict(values=[info_df[col] for col in info_df.columns], fill_color='lavender', align='left')
            )])
            fig.update_layout(title='Информация о данных', margin=dict(l=10, r=10, b=10, t=50))
            self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка при создании информации о данных: {str(e)}")
    def add_boxplot(self, column):
        try:
            fig = go.Figure()
            fig.add_trace(go.Box(
                y=self.data[column].dropna(),
                name=column,
                boxpoints='outliers',
                marker_color='lightblue',
                line_color='blue'
            ))
            fig.update_layout(
                title=f'Диаграмма размаха: {column}',
                yaxis_title=column,
                showlegend=False,
                margin=dict(l=40, r=30, b=80, t=100),
                plot_bgcolor='rgba(240,240,240,0.95)'
            )
            self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка при создании диаграммы размаха: {str(e)}")
    def add_histogram(self, column, bins=10):
        try:
            fig = px.histogram(self.data, x=column, nbins=bins, title=f'Гистограмма: {column}')
            fig.update_layout(
                xaxis_title=column,
                yaxis_title='Частота',
                bargap=0.1,
                margin=dict(l=60, r=30, b=60, t=80)
            )
            self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка при создании гистограммы: {str(e)}")
    def add_bar_chart(self):
        try:
            numeric_cols = self.data.select_dtypes(include=['number']).columns
            category_cols = self.data.select_dtypes(include=['category', 'object']).columns
            if len(numeric_cols) >= 1 and len(category_cols) >= 1:
                col_num = numeric_cols[0]
                col_cat = category_cols[0]
                fig = px.bar(
                    self.data.groupby(col_cat, observed=True)[col_num].mean().reset_index(),
                    x=col_cat,
                    y=col_num,
                    title=f'Столбчатая диаграмма: {col_num} по {col_cat}'
                )
                fig.update_layout(
                    xaxis_title=col_cat,
                    yaxis_title=f'Среднее {col_num}',
                    margin=dict(l=60, r=30, b=100, t=80)
                )
                self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка столбчатой диаграммы: {str(e)}")
    def add_pie_chart(self):
        try:
            category_cols = self.data.select_dtypes(include=['category', 'object']).columns
            if len(category_cols) >= 1:
                col = category_cols[0]
                counts = self.data[col].value_counts().reset_index()
                counts.columns = ['category', 'count']
                if len(counts) > 10:
                    others = counts[10:]['count'].sum()
                    counts = counts[:10]
                    counts.loc[len(counts)] = ['Другие', others]
                fig = px.pie(
                    counts,
                    values='count',
                    names='category',
                    title=f'Круговая диаграмма: {col}'
                )
                fig.update_traces(
                    textposition='inside',
                    textinfo='percent+label',
                    pull=[0.1 if i == 0 else 0 for i in range(len(counts))]
                )
                fig.update_layout(
                    margin=dict(l=30, r=30, b=30, t=80),
                    showlegend=False
                )
                self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка круговой диаграммы: {str(e)}")
    def add_scatter(self, x_col, y_col):
        try:
            fig = px.scatter(
                self.data,
                x=x_col,
                y=y_col,
                title=f'Диаграмма рассеивания: {x_col} vs {y_col}'
            )
            fig.update_layout(
                xaxis_title=x_col,
                yaxis_title=y_col,
                margin=dict(l=60, r=30, b=60, t=80)
            )
            self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка при создании диаграммы рассеивания: {str(e)}")
    def add_line_chart(self):
        try:
            numeric_cols = self.data.select_dtypes(include=['number']).columns
            if len(numeric_cols) >= 1:
                fig = px.line(
                    self.data,
                    y=numeric_cols[0],
                    title=f'Линейный график: {numeric_cols[0]}'
                )
                fig.update_layout(
                    xaxis_title='Индекс',
                    yaxis_title=numeric_cols[0],
                    margin=dict(l=60, r=30, b=60, t=80)
                )
                self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка при создании линейного графика: {str(e)}")
    def add_violin_plot(self):
        try:
            numeric_cols = self.data.select_dtypes(include=['number']).columns
            category_cols = self.data.select_dtypes(include=['category', 'object']).columns
            if len(numeric_cols) >= 1 and len(category_cols) >= 1:
                num_col = numeric_cols[0]
                cat_col = category_cols[0]
                unique_cats = self.data[cat_col].unique()[:10]  # Ограничиваем количество категорий
                fig = go.Figure()
                for cat in unique_cats:
                    fig.add_trace(go.Violin(
                        y=self.data[self.data[cat_col] == cat][num_col].dropna(),
                        name=str(cat),
                        box_visible=True,
                        meanline_visible=True
                    ))
                fig.update_layout(
                    title=f'Диаграмма скрипки: {num_col}',
                    yaxis_title=num_col,
                    margin=dict(l=60, r=30, b=80, t=80),
                    violinmode='group'
                )
                self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка диаграммы скрипки: {str(e)}")
    def add_scatter_matrix(self):
        try:
            numeric_cols = self.data.select_dtypes(include=['number']).columns
            if len(numeric_cols) >= 2:
                cols = numeric_cols[:4]  # Ограничиваем количество столбцов
                fig = px.scatter_matrix(
                    self.data,
                    dimensions=cols,
                    title="Матрица диаграмм рассеивания"
                )
                fig.update_layout(
                    margin=dict(l=20, r=20, b=80, t=100),
                    height=800
                )
                self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка при создании матрицы диаграмм рассеивания: {str(e)}")
    def add_correlation_matrix(self):
        try:
            numeric_cols = self.data.select_dtypes(include=['number']).columns
            if len(numeric_cols) >= 2:
                corr = self.data[numeric_cols].corr(numeric_only=True)
                fig = go.Figure(data=go.Heatmap(
                    z=corr.values,
                    x=corr.columns,
                    y=corr.index,
                    colorscale='RdBu',
                    zmin=-1,
                    zmax=1,
                    hoverongaps=False
                ))
                fig.update_layout(
                    title="Матрица корреляций",
                    xaxis_title="Переменные",
                    yaxis_title="Переменные",
                    margin=dict(l=100, r=30, b=100, t=80),
                    height=600
                )
                self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка матрицы корреляций: {str(e)}")
    def add_3d_plot(self):
        try:
            numeric_cols = self.data.select_dtypes(include=['number']).columns
            if len(numeric_cols) >= 3:
                fig = px.scatter_3d(
                    self.data,
                    x=numeric_cols[0],
                    y=numeric_cols[1],
                    z=numeric_cols[2],
                    color=numeric_cols[2],
                    title=f'3D график: {numeric_cols[0]} vs {numeric_cols[1]} vs {numeric_cols[2]}'
                )
                fig.update_layout(
                    margin=dict(l=0, r=0, b=0, t=30),
                    scene=dict(
                        xaxis_title=numeric_cols[0],
                        yaxis_title=numeric_cols[1],
                        zaxis_title=numeric_cols[2]
                    )
                )
                self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка при создании 3D графика: {str(e)}")
    def add_heatmap(self):
        try:
            numeric_cols = self.data.select_dtypes(include=['number']).columns.tolist()
            if len(numeric_cols) >= 2:
                df = self.data[numeric_cols].corr()
                fig = go.Figure(data=go.Heatmap(
                    z=df.values,
                    x=df.columns,
                    y=df.index,
                    colorscale='Viridis',
                    showscale=True
                ))
                fig.update_layout(title='Тепловая карта корреляций')
                self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка создания тепловой карты: {str(e)}")
    def add_radar_chart(self):
        try:
            numeric_cols = self.data.select_dtypes(include=['number']).columns.tolist()
            category_cols = self.data.select_dtypes(include=['category', 'object']).columns.tolist()
            if len(numeric_cols) >= 1 and len(category_cols) >= 1:
                col_num = numeric_cols[0]
                col_cat = category_cols[0]
                grouped = self.data.groupby(col_cat, observed=True)[col_num].mean().reset_index()
                categories = grouped[col_cat].unique()
                values = grouped[col_num].values
                fig = go.Figure()
                fig.add_trace(go.Scatterpolar(
                    r=np.concatenate((values, [values[0]])),
                    theta=np.concatenate((categories, [categories[0]])),
                    fill='toself',
                    name=col_num
                ))
                fig.update_layout(polar=dict(radialaxis=dict(visible=True)), showlegend=False)
                fig.update_layout(title=f'Радарная диаграмма: {col_num} по {col_cat}')
                self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка создания радарной диаграммы: {str(e)}")
    def add_time_series(self, time_col, value_col):
        try:
            df = self.data[[time_col, value_col]]
            df[time_col] = pd.to_datetime(df[time_col], errors='coerce')
            df = df.dropna().sort_values(time_col)
            fig = px.line(df, x=time_col, y=value_col, title=f'График временного ряда: {value_col} по {time_col}')
            self.figures.append(fig)
        except Exception as e:
            print(f"Ошибка создания временного ряда: {str(e)}")
    def figure_to_html_img(self, fig):
        try:
            if plt is not None and isinstance(fig, plt.Figure):
                buf = io.BytesIO()
                fig.savefig(buf, format='png', bbox_inches='tight', dpi=150, pad_inches=0.5)
                plt.close(fig)
                buf.seek(0)
                img_str = base64.b64encode(buf.read()).decode('utf-8')
                return f'<img src="data:image/png;base64,{img_str}" style="max-width:100%; height:auto;">'
            else:
                return fig.to_html(full_html=False, include_plotlyjs='cdn')
        except Exception as e:
            print(f"Ошибка конвертации графика: {str(e)}")
            return ""
    def generate_report(self):
        if not self.figures:
            raise ValueError("Невозможно сгенерировать отчет: не создано ни одной визуализации.")
        plot_htmls = []
        for i, fig in enumerate(self.figures, 1):
            plot_html = self.figure_to_html_img(fig)
            title = fig.layout.title.text if hasattr(fig, 'layout') and fig.layout.title.text else f"График {i}"
            plot_htmls.append(f"""
                <div class="plot-container">
                    <div class="plot-title">{title}</div>
                    {plot_html}
                </div>
            """)
        html_content = f"""
        <!DOCTYPE html>
        <html lang="ru">
        <head>
            <meta charset="UTF-8">
            <title>Отчет анализа данных</title>
            <script src="https://cdn.plot.ly/plotly-latest.min.js"></script>  
            <style>
                body {{
                    font-family: Arial, sans-serif;
                    margin: 20px;
                    line-height: 1.6;
                }}
                h1, h2 {{
                    color: #2b5278;
                    margin-bottom: 15px;
                }}
                .report-header {{
                    background-color: #f5f5f5;
                    padding: 20px;
                    border-radius: 5px;
                    margin-bottom: 20px;
                }}
                .plot-container {{
                    margin: 30px 0;
                    border: 1px solid #e0e0e0;
                    border-radius: 5px;
                    padding: 15px;
                    background-color: #f9f9f9;
                }}
                .plot-title {{
                    font-size: 18px;
                    font-weight: bold;
                    margin-bottom: 15px;
                    color: #2b5278;
                }}
                .plotly-graph-div {{
                    width: 100%;
                    height: 600px;
                }}
                table {{
                    width: 100%;
                    border-collapse: collapse;
                    margin: 15px 0;
                }}
                th, td {{
                    border: 1px solid #ddd;
                    padding: 8px;
                    text-align: left;
                }}
                th {{
                    background-color: #f2f2f2;
                }}
            </style>
        </head>
        <body>
            <div class="report-header">
                <h1>Отчет анализа данных</h1>
                <p><strong>Сгенерировано:</strong> {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}</p>
                <p><strong>Источник данных:</strong> {self.file_path}</p>
                <p><strong>Размер данных:</strong> {len(self.data)} строк, {len(self.data.columns)} столбцов</p>
                <p><strong>Использовано памяти:</strong> {self.data.memory_usage(deep=True).sum() / 1024 / 1024:.2f} MB</p>
            </div>
            {"<hr>".join(plot_htmls)}
        </body>
        </html>
        """
        with tempfile.NamedTemporaryFile(mode='w', suffix='.html', delete=False, encoding='utf-8') as f:
            f.write(html_content)
            temp_path = f.name
        webbrowser.open(f"file://{temp_path}")
        return temp_path