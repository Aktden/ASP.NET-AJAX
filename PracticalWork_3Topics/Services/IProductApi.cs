using PracticalWork_3Topics.Dtos;

namespace PracticalWork_3Topics.Services;

public interface IProductApi
{
    Task<ProductDto?> GetProductAsync(int id, CancellationToken ct);
    Task<IReadOnlyList<ProductDto>> SearchAsync(string query, CancellationToken ct);
}
