using AspNetAjaxLifecycle.Middleware;
using AspNetAjaxLifecycle.Models;

var builder = WebApplication.CreateBuilder(args);
Console.WriteLine("1. Создан WebApplicationBuilder");

var app = builder.Build();
Console.WriteLine("2. Создан объект WebApplication");

app.UseMiddleware<RequestLoggingMiddleware>();
Console.WriteLine("3. Middleware зарегистрирован");

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/api/status", () =>
{
    Console.WriteLine("4. Выполняется GET /api/status");

    return Results.Ok(new
    {
        message = "Приложение работает"
    });
});

app.MapPost("/api/form", (UserFormModel model) =>
{
    Console.WriteLine("4. Выполняется POST /api/form");
    Console.WriteLine($"Имя: {model.Name}");
    Console.WriteLine($"Email: {model.Email}");
    Console.WriteLine($"Сообщение: {model.Message}");

    return Results.Ok(new
    {
        success = true,
        message = $"Данные пользователя {model.Name} успешно получены"
    });
});

Console.WriteLine("5. Маршруты приложения зарегистрированы");

app.Lifetime.ApplicationStarted.Register(() =>
{
    Console.WriteLine("6. Приложение запущено");
});

app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("7. Приложение завершает работу");
});

app.Lifetime.ApplicationStopped.Register(() =>
{
    Console.WriteLine("8. Приложение остановлено");
});

app.Run();
