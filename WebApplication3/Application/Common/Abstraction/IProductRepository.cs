using Shop.Domain;

namespace Shop.Application.Common.Abstractions;

public interface IProductRepository
{
    Task<Product?> GetAsync(Guid id, CancellationToken ct);
    void Add(Product product);
}