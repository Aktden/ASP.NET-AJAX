# ASP.NET Core AJAX Lifecycle

Лабораторная работа по дисциплине **«Разработка веб-приложений с использованием технологий ASP.NET и AJAX»**.

## Цель работы

Создать веб-приложение ASP.NET Core на основе пустого шаблона, реализовать HTML-форму, AJAX-запрос, собственный Middleware и показать жизненный цикл приложения и HTTP-запроса.

## Использованные технологии

- ASP.NET Core 8
- C#
- HTML / CSS
- JavaScript
- AJAX через Fetch API
- Middleware
- Minimal API

## Структура проекта

```text
AspNetAjaxLifecycle/
├── Middleware/
│   └── RequestLoggingMiddleware.cs
├── Models/
│   └── UserFormModel.cs
├── wwwroot/
│   └── index.html
├── Screenshots/
│   ├── 01-form.png
│   ├── 02-lifecycle.png
│   ├── 03-program-code.png
│   └── 04-middleware-code.png
├── Program.cs
├── AspNetAjaxLifecycle.csproj
└── README.md
```

## Форма и AJAX

Пользователь вводит имя, email и сообщение. JavaScript перехватывает отправку формы и отправляет JSON на сервер через `fetch()` без перезагрузки страницы.

Маршрут:

```text
POST /api/form
```

Пример отправляемого JSON:

```json
{
  "name": "Иван",
  "email": "ivan@example.com",
  "message": "Привет"
}
```

## Middleware

`RequestLoggingMiddleware` перехватывает каждый HTTP-запрос до выполнения конечной точки и после неё. В консоль выводятся HTTP-метод, путь, время запроса и код ответа.

## Жизненный цикл приложения

1. `WebApplication.CreateBuilder(args)` — создание конфигурации приложения.
2. `builder.Build()` — создание экземпляра `WebApplication`.
3. Регистрация Middleware.
4. Регистрация статических файлов и API-маршрутов.
5. `app.Run()` — запуск веб-сервера.
6. Срабатывает `ApplicationStarted`.
7. Приложение принимает HTTP-запросы.
8. При завершении срабатывает `ApplicationStopping`.
9. После полной остановки срабатывает `ApplicationStopped`.

## Жизненный цикл HTTP-запроса

```text
Браузер
   │
   │ AJAX POST /api/form
   ▼
Kestrel
   ▼
ASP.NET Core Pipeline
   ▼
RequestLoggingMiddleware
   │
   │ await _next(context)
   ▼
Routing
   ▼
POST /api/form
   ▼
JSON → UserFormModel
   ▼
Обработка данных
   ▼
JSON Response
   ▼
RequestLoggingMiddleware
   ▼
Kestrel
   ▼
Браузер
   ▼
JavaScript обновляет HTML без перезагрузки
```

## Запуск

Требуется .NET 8 SDK.

```bash
dotnet restore
dotnet run
```

После запуска открыть адрес, который появится в терминале, например:

```text
http://localhost:5000
```

## Что показать преподавателю

1. Файл `Program.cs`.
2. Собственный `RequestLoggingMiddleware.cs`.
3. Форму в `wwwroot/index.html`.
4. AJAX-запрос через `fetch()`.
5. Схему жизненного цикла из папки `Screenshots`.
6. При запуске проекта — сообщения Middleware в консоли и запрос `POST /api/form` в DevTools → Network.
