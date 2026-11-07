using Microsoft.EntityFrameworkCore;
using PracticalWork_3Topics.Domain;

namespace PracticalWork_3Topics.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Customer>()
            .HasMany(customer => customer.Orders)
            .WithOne(order => order.Customer)
            .HasForeignKey(order => order.CustomerId);

        modelBuilder.Entity<Order>()
            .HasMany(order => order.Items)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId);

        modelBuilder.Entity<Customer>().Property(customer => customer.Name).HasMaxLength(120);
        modelBuilder.Entity<OrderItem>().Property(item => item.ProductName).HasMaxLength(160);
    }
}
