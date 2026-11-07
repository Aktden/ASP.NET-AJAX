namespace PracticalWork_3Topics.Domain;

public sealed class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public double Total { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
