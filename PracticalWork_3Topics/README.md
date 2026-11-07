# Практическая работа: SQL, HttpClientFactory, Resilience и идемпотентность

## Что реализовано

### 1. SQL и Entity Framework Core

- `GET /api/reports/orders/bad` — намеренно неоптимальный вариант N+1: сначала загружаются клиенты, затем для каждого клиента отдельно запрашиваются заказы.
- `GET /api/reports/orders/good` — оптимизированная проекция: выбираются только необходимые поля, `COUNT` и `SUM` выполняются на стороне БД.
- `GET /api/reports/customers/with-items` — загрузка связанных сущностей через `Include`, `ThenInclude` и `AsSplitQuery`.
- `GET /api/reports/top-customers?days=30` — отчёт топ-5 клиентов с оконной функцией `RANK()` через Dapper и параметризованный SQL.
- `GET /api/reports/benchmark` — пример измерения времени двух вариантов. Это демонстрационный замер, а не полноценный статистический бенчмарк.

Для простоты используется SQLite и автоматически создаваемая база `practical-work.db`. В учебном примере суммы представлены типом `double`, чтобы агрегаты корректно выполнялись SQLite.

### 2. HttpClientFactory

- Типизированный клиент `IProductApi` / `ProductApi`.
- Настройка `BaseAddress`, таймаута и заголовка `Accept`.
- `CorrelationHandler` добавляет `X-Correlation-Id` и записывает длительность HTTP-запроса в лог.
- `AddStandardResilienceHandler()` добавляет стандартную обработку временных HTTP-сбоев.

Эндпоинты:
- `GET /api/external/products/1`
- `GET /api/external/products/search?q=phone`

Эти два запроса обращаются к публичному учебному API DummyJSON, поэтому для них нужен интернет.

### 3. Resilience и идемпотентность

- Отдельный HTTP-клиент для имитации платёжного API.
- `Retry` с экспоненциальной задержкой и jitter.
- `Circuit Breaker` с порогом ошибок и временем восстановления.
- Таймауты на общий вызов и отдельную попытку.
- Middleware читает `Idempotency-Key`, сохраняет успешный ответ на 24 часа и возвращает тот же ответ при повторном запросе с тем же ключом.

Эндпоинт:
- `POST /api/payments`

Пример успешного запроса:

```http
POST http://localhost:5000/api/payments
Content-Type: application/json
Idempotency-Key: demo-payment-001

{
  "orderId": "ORDER-1001",
  "amount": 100,
  "currency": "USD"
}
```

Повторите запрос с тем же `Idempotency-Key`: middleware вернёт ранее сохранённый успешный ответ, не запуская обработчик повторно.

Чтобы увидеть повторы Retry, отправьте запрос на `/api/payments?fail=true`. Имитатор будет возвращать `503 Service Unavailable`; реальных платежей проект не выполняет.

## Запуск

Нужен .NET 8 SDK.

Из папки проекта:

```bash
dotnet restore
dotnet run --urls http://localhost:5000
```

Откройте `http://localhost:5000/api/info` — там перечислены все маршруты.


