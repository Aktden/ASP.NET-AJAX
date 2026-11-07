using System.Net;
using Microsoft.EntityFrameworkCore;
using PracticalWork_3Topics.Data;
using PracticalWork_3Topics.Domain;
using PracticalWork_3Topics.Dtos;
using PracticalWork_3Topics.Middleware;
using PracticalWork_3Topics.Services;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.Http.Resilience;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? "Data Source=practical-work.db";

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<IdempotencyLock>();
builder.Services.AddScoped<OrderReportService>();
builder.Services.AddTransient<CorrelationHandler>();

// Тема 2: типизированный HttpClient с логированием и стандартной обработкой сбоев.
builder.Services.AddHttpClient<IProductApi, ProductApi>(client =>
{
    client.BaseAddress = new Uri("https://dummyjson.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("Accept", "application/json");
})
.AddHttpMessageHandler<CorrelationHandler>()
.AddStandardResilienceHandler();

// Тема 3: отдельный HTTP-клиент для демонстрации Retry и Circuit Breaker.
// Вместо настоящего платёжного сервиса используется локальный имитатор ответа.
builder.Services.AddHttpClient<IPaymentApi, PaymentApi>(client =>
{
    client.BaseAddress = new Uri("https://payments.example.test/");
    client.Timeout = Timeout.InfiniteTimeSpan;
})
.ConfigurePrimaryHttpMessageHandler(() => new SimulatedPaymentHandler())
.AddResilienceHandler("payment-pipeline", pipeline =>
{
    pipeline.AddTimeout(TimeSpan.FromSeconds(5));

    pipeline.AddRetry(new HttpRetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        Delay = TimeSpan.FromMilliseconds(300),
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(response =>
                (int)response.StatusCode >= 500 ||
                response.StatusCode == HttpStatusCode.RequestTimeout ||
                response.StatusCode == HttpStatusCode.TooManyRequests)
    });

    pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 10,
        BreakDuration = TimeSpan.FromSeconds(15),
        ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
            .Handle<HttpRequestException>()
            .HandleResult(response => (int)response.StatusCode >= 500)
    });

    pipeline.AddTimeout(TimeSpan.FromSeconds(3));
});

var app = builder.Build();

// Создаём учебную БД и заполняем её демонстрационными данными при первом запуске.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(db);
}

app.UseMiddleware<IdempotencyMiddleware>();

app.MapGet("/", () => Results.Ok(new
{
    application = "Практическая работа по трём темам",
    topics = new[] { "SQL и EF Core", "HttpClientFactory", "Resilience и идемпотентность" },
    documentation = "/api/info"
}));

app.MapGet("/api/info", () => Results.Ok(new
{
    sql = new[]
    {
        "GET /api/reports/orders/bad — демонстрация N+1",
        "GET /api/reports/orders/good — проекция и агрегаты на стороне SQL",
        "GET /api/reports/customers/with-items — Include + AsSplitQuery",
        "GET /api/reports/top-customers?days=30 — Dapper и оконная функция RANK",
        "GET /api/reports/benchmark — сравнение времени двух вариантов"
    },
    httpClient = new[]
    {
        "GET /api/external/products/1 — запрос через типизированный HttpClient",
        "GET /api/external/products/search?q=phone — поиск во внешнем API"
    },
    resilience = new[]
    {
        "POST /api/payments — демонстрация Retry и Circuit Breaker",
        "Повторите POST с тем же заголовком Idempotency-Key, чтобы получить сохранённый ответ"
    }
}));

// Тема 1: SQL / EF Core.
app.MapGet("/api/reports/orders/bad", async (OrderReportService reports, CancellationToken ct) =>
    Results.Ok(await reports.GetOrdersBadAsync(ct)));

app.MapGet("/api/reports/orders/good", async (OrderReportService reports, CancellationToken ct) =>
    Results.Ok(await reports.GetOrdersGoodAsync(ct)));

app.MapGet("/api/reports/customers/with-items", async (AppDbContext db, CancellationToken ct) =>
{
    var customers = await db.Customers
        .Include(customer => customer.Orders)
            .ThenInclude(order => order.Items)
        .AsSplitQuery()
        .AsNoTracking()
        .ToListAsync(ct);

    var result = customers.Select(customer => new
    {
        customer.Id,
        customer.Name,
        Orders = customer.Orders.Select(order => new
        {
            order.Id,
            order.CreatedAt,
            order.Total,
            Items = order.Items.Select(item => new
            {
                item.Id,
                item.ProductName,
                item.Quantity,
                item.UnitPrice
            })
        })
    });

    return Results.Ok(result);
});

app.MapGet("/api/reports/top-customers", async (
    OrderReportService reports,
    int? days,
    CancellationToken ct) =>
{
    var period = Math.Clamp(days ?? 30, 1, 3650);
    var result = await reports.GetTopCustomersAsync(DateTime.UtcNow.AddDays(-period), ct);
    return Results.Ok(result);
});

app.MapGet("/api/reports/benchmark", async (OrderReportService reports, CancellationToken ct) =>
{
    var badWatch = System.Diagnostics.Stopwatch.StartNew();
    var bad = await reports.GetOrdersBadAsync(ct);
    badWatch.Stop();

    var goodWatch = System.Diagnostics.Stopwatch.StartNew();
    var good = await reports.GetOrdersGoodAsync(ct);
    goodWatch.Stop();

    return Results.Ok(new
    {
        badMilliseconds = badWatch.ElapsedMilliseconds,
        goodMilliseconds = goodWatch.ElapsedMilliseconds,
        customersInBadResult = bad.Count,
        customersInGoodResult = good.Count,
        note = "Результат зависит от размера БД, оборудования и прогрева запросов. Один запуск не является полноценным бенчмарком."
    });
});

// Тема 2: HttpClientFactory.
app.MapGet("/api/external/products/{id:int}", async (
    int id,
    IProductApi productApi,
    CancellationToken ct) =>
{
    var product = await productApi.GetProductAsync(id, ct);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapGet("/api/external/products/search", async (
    string q,
    IProductApi productApi,
    CancellationToken ct) => Results.Ok(await productApi.SearchAsync(q, ct)));

// Тема 3: внешний платёжный вызов защищён Retry / Circuit Breaker.
// Добавьте ?fail=true, чтобы имитатор постоянно возвращал 503 и можно было увидеть Retry.
app.MapPost("/api/payments", async (
    PaymentRequest request,
    HttpContext context,
    IPaymentApi paymentApi,
    CancellationToken ct) =>
{
    var result = await paymentApi.PayAsync(request, context.Request.Query["fail"] == "true", ct);
    return Results.Ok(result);
});

app.Run();
