namespace Shop.Domain;

public class Order
{
    public Guid Id { get; private set; }
    public string CustomerEmail { get; private set; } = default!;
    public List<OrderItem> Items { get; private set; } = new();
    public decimal Total => Items.Sum(i => i.Price * i.Quantity);

    private Order() { }

    public Order(string customerEmail, IEnumerable<OrderItem> items)
    {
        Id = Guid.NewGuid();
        CustomerEmail = customerEmail;
        Items = items.ToList();
    }
}

public record OrderItem(Guid ProductId, string Name, decimal Price, int Quantity);

public bool IsShipped { get; private set; }
public bool IsCancelled { get; private set; }

public void Cancel()
{
    if (IsShipped) throw new InvalidOperationException("Cannot cancel shipped order");
    IsCancelled = true;
}