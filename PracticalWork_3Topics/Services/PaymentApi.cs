using System.Net.Http.Json;
using PracticalWork_3Topics.Dtos;

namespace PracticalWork_3Topics.Services;

public sealed class PaymentApi(HttpClient http) : IPaymentApi
{
    public async Task<PaymentResult> PayAsync(
        PaymentRequest request,
        bool simulateFailure,
        CancellationToken ct)
    {
        var path = simulateFailure ? "payments?fail=true" : "payments";
        using var response = await http.PostAsJsonAsync(path, request, ct);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PaymentResult>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Платёжный сервис вернул пустой ответ.");
    }
}
