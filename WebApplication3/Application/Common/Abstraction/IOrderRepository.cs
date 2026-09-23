namespace Shop.Application.Orders.CreateOrder;

public sealed record CreateOrderCommand(
    string CustomerEmail,
    IReadOnlyList<CreateOrderItem> Items);

public sealed record CreateOrderItem(Guid ProductId, int Quantity);