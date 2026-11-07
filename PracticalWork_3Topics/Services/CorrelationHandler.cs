using System.Diagnostics;

namespace PracticalWork_3Topics.Services;

public sealed class CorrelationHandler(ILogger<CorrelationHandler> logger) : DelegatingHandler
{
    private const string HeaderName = "X-Correlation-Id";

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString("N");
        request.Headers.TryAddWithoutValidation(HeaderName, correlationId);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();
            logger.LogInformation(
                "HTTP {Method} {Url} -> {Status} за {ElapsedMs} мс, CorrelationId={CorrelationId}",
                request.Method,
                request.RequestUri,
                (int)response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            logger.LogError(exception,
                "HTTP {Method} {Url} завершился ошибкой за {ElapsedMs} мс, CorrelationId={CorrelationId}",
                request.Method,
                request.RequestUri,
                stopwatch.ElapsedMilliseconds,
                correlationId);
            throw;
        }
    }
}
