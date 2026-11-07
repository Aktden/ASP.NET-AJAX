namespace PracticalWork_3Topics.Dtos;

public sealed record PaymentRequest(string OrderId, double Amount, string Currency = "USD");

public sealed record PaymentResult(string TransactionId, string Status, double Amount, string Currency);
