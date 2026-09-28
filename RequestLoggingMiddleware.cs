namespace AspNetAjaxLifecycle.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;

    public RequestLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        Console.WriteLine("------------------------------------");
        Console.WriteLine("Middleware: запрос получен");
        Console.WriteLine($"Метод: {context.Request.Method}");
        Console.WriteLine($"Путь: {context.Request.Path}");
        Console.WriteLine($"Время: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");

        await _next(context);

        Console.WriteLine("Middleware: ответ сформирован");
        Console.WriteLine($"Статус ответа: {context.Response.StatusCode}");
        Console.WriteLine("------------------------------------");
    }
}
