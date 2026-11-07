using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace PracticalWork_3Topics.Middleware;

public sealed class IdempotencyMiddleware(
    RequestDelegate next,
    IDistributedCache cache,
    IdempotencyLock locks,
    ILogger<IdempotencyMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Method != HttpMethods.Post &&
            context.Request.Method != HttpMethods.Patch)
        {
            await next(context);
            return;
        }

        var idempotencyKey = context.Request.Headers["Idempotency-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            await next(context);
            return;
        }

        var cacheKey = $"idem:{context.Request.Method}:{context.Request.Path}:{idempotencyKey}";
        var gate = locks.For(cacheKey);
        await gate.WaitAsync(context.RequestAborted);

        try
        {
            var cachedJson = await cache.GetStringAsync(cacheKey, context.RequestAborted);
            if (cachedJson is not null)
            {
                var cached = JsonSerializer.Deserialize<CachedResponse>(cachedJson);
                if (cached is not null)
                {
                    logger.LogInformation("Возвращён сохранённый ответ для Idempotency-Key={Key}", idempotencyKey);
                    context.Response.StatusCode = cached.StatusCode;
                    context.Response.ContentType = cached.ContentType;
                    await context.Response.WriteAsync(cached.Body, context.RequestAborted);
                    return;
                }
            }

            var originalBody = context.Response.Body;
            await using var buffer = new MemoryStream();
            context.Response.Body = buffer;

            try
            {
                await next(context);
                buffer.Position = 0;
                var body = await new StreamReader(buffer).ReadToEndAsync(context.RequestAborted);

                if (context.Response.StatusCode is >= 200 and < 300)
                {
                    var value = new CachedResponse(
                        context.Response.StatusCode,
                        context.Response.ContentType,
                        body);

                    await cache.SetStringAsync(
                        cacheKey,
                        JsonSerializer.Serialize(value),
                        new DistributedCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
                        },
                        context.RequestAborted);
                }

                buffer.Position = 0;
                await buffer.CopyToAsync(originalBody, context.RequestAborted);
            }
            finally
            {
                context.Response.Body = originalBody;
            }
        }
        finally
        {
            gate.Release();
        }
    }

    private sealed record CachedResponse(int StatusCode, string? ContentType, string Body);
}
