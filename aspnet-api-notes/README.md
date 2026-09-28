# ASP.NET Core API — код с пары

Репозиторий собран из конспекта `конспект код с пары (2).docx`.

## Что внутри

- `http/etag-idempotency.http` — ETag, If-None-Match, If-Match, Idempotency-Key, REST-методы.
- `http/antipatterns.md` — отмеченные в конспекте REST-антипаттерны.
- `aspnet/BindingExamples.cs` — model binding, источники данных, custom binder.
- `aspnet/DtoExamples.cs` — DTO и mapping.
- `aspnet/ValidationExamples.cs` — DataAnnotations, свой валидатор, IValidatableObject, FluentValidation.
- `aspnet/ProblemDetailsExamples.cs` — ProblemDetails и ValidationProblem.
- `aspnet/CustomProblemDetailsFactory.cs` — кастомизация ProblemDetailsFactory.
- `aspnet/GlobalExceptionHandler.cs` — глобальная обработка исключений.
- `aspnet/ServiceRegistration.cs` — регистрации сервисов из конспекта.

> Важно: это набор учебных примеров из конспекта, а не готовый цельный проект.
> В исходном конспекте некоторые типы (`Product`, `ProductResponse`, `CustomerDto`,
> `OrderItemDto` и др.) не определены, поэтому отдельные фрагменты требуют контекста
> вашего проекта.
