using System.Net;
using System.Net.Http.Json;

namespace PracticalWork_3Topics.Services;

// Учебная заглушка: не отправляет реальные платежи и не обращается в интернет.
public sealed class SimulatedPaymentHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var shouldFail = request.RequestUri?.Query.Contains("fail=true", StringComparison.OrdinalIgnoreCase) == true;

        if (shouldFail)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
            {
                Content = JsonContent.Create(new { error = "Учебная имитация временной недоступности" })
            });
        }

        var result = new
        {
            transactionId = Guid.NewGuid().ToString("N"),
            status = "approved",
            amount = 100.0,
            currency = "USD"
        };

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(result)
        });
    }
}
