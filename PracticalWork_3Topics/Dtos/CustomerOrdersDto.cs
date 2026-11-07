namespace PracticalWork_3Topics.Dtos;

public sealed class CustomerOrdersDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int OrdersCount { get; set; }
    public double TotalSum { get; set; }
}
