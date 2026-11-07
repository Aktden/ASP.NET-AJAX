using Microsoft.EntityFrameworkCore;
using PracticalWork_3Topics.Domain;

namespace PracticalWork_3Topics.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Customers.AnyAsync())
            return;

        var today = DateTime.UtcNow.Date;
        var customers = new List<Customer>
        {
            new() { Name = "Иван Петров" },
            new() { Name = "Анна Смирнова" },
            new() { Name = "Дмитрий Соколов" },
            new() { Name = "Мария Кузнецова" },
            new() { Name = "Алексей Волков" }
        };

        db.Customers.AddRange(customers);
        await db.SaveChangesAsync();

        var orders = new List<Order>();
        for (var customerIndex = 0; customerIndex < customers.Count; customerIndex++)
        {
            for (var orderIndex = 0; orderIndex < customerIndex + 2; orderIndex++)
            {
                var order = new Order
                {
                    CustomerId = customers[customerIndex].Id,
                    CreatedAt = today.AddDays(-(customerIndex * 3 + orderIndex * 2)),
                    Total = (customerIndex + 1) * 125.50 + (orderIndex + 1) * 49.90
                };

                order.Items.Add(new OrderItem
                {
                    ProductName = "Товар A",
                    Quantity = orderIndex + 1,
                    UnitPrice = 49.90
                });
                order.Items.Add(new OrderItem
                {
                    ProductName = "Товар B",
                    Quantity = 1,
                    UnitPrice = 125.50
                });
                orders.Add(order);
            }
        }

        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();
    }
}
