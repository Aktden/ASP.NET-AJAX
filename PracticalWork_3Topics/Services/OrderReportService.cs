using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PracticalWork_3Topics.Data;
using PracticalWork_3Topics.Dtos;

namespace PracticalWork_3Topics.Services;

public sealed class OrderReportService(AppDbContext db, IConfiguration configuration)
{
    // Неэффективный вариант: один запрос за клиентами + отдельный запрос заказов для каждого клиента.
    public async Task<List<CustomerOrdersDto>> GetOrdersBadAsync(CancellationToken ct)
    {
        var customers = await db.Customers.AsNoTracking().ToListAsync(ct);
        var result = new List<CustomerOrdersDto>();

        foreach (var customer in customers)
        {
            var orders = await db.Orders
                .AsNoTracking()
                .Where(order => order.CustomerId == customer.Id)
                .ToListAsync(ct);

            result.Add(new CustomerOrdersDto
            {
                Id = customer.Id,
                Name = customer.Name,
                OrdersCount = orders.Count,
                TotalSum = orders.Sum(order => order.Total)
            });
        }

        return result;
    }

    // Оптимизированный вариант: одна проекция, COUNT и SUM выполняются в базе данных.
    public Task<List<CustomerOrdersDto>> GetOrdersGoodAsync(CancellationToken ct)
    {
        return db.Customers
            .AsNoTracking()
            .Select(customer => new CustomerOrdersDto
            {
                Id = customer.Id,
                Name = customer.Name,
                OrdersCount = customer.Orders.Count,
                TotalSum = customer.Orders.Sum(order => (double?)order.Total) ?? 0d
            })
            .ToListAsync(ct);
    }

    // Сложный отчёт: Dapper + SQL-оконная функция RANK.
    public async Task<IReadOnlyList<TopCustomerDto>> GetTopCustomersAsync(DateTime since, CancellationToken ct)
    {
        const string sql = """
            SELECT c.Id,
                   c.Name,
                   SUM(o.Total) AS Total,
                   RANK() OVER (ORDER BY SUM(o.Total) DESC) AS Rank
            FROM Customers AS c
            INNER JOIN Orders AS o ON o.CustomerId = c.Id
            WHERE o.CreatedAt >= @Since
            GROUP BY c.Id, c.Name
            ORDER BY Total DESC
            LIMIT 5;
            """;

        var connectionString = configuration.GetConnectionString("Default")
            ?? "Data Source=practical-work.db";

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync(ct);

        var rows = await connection.QueryAsync<TopCustomerDto>(
            new CommandDefinition(sql, new { Since = since }, cancellationToken: ct));

        return rows.AsList();
    }
}
