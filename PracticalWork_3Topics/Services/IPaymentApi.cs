using PracticalWork_3Topics.Dtos;

namespace PracticalWork_3Topics.Services;

public interface IPaymentApi
{
    Task<PaymentResult> PayAsync(PaymentRequest request, bool simulateFailure, CancellationToken ct);
}
