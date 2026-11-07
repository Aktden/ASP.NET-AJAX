using System.Net;
using System.Net.Http.Json;
using PracticalWork_3Topics.Dtos;

namespace PracticalWork_3Topics.Services;

public sealed class ProductApi(HttpClient http, ILogger<ProductApi> logger) : IProductApi
{
    public async Task<ProductDto?> GetProductAsync(int id, CancellationToken ct)
    {
        using var response = await http.GetAsync($"products/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProductDto>(cancellationToken: ct);
    }

    public async Task<IReadOnlyList<ProductDto>> SearchAsync(string query, CancellationToken ct)
    {
        var escapedQuery = Uri.EscapeDataString(query);
        using var response = await http.GetAsync($"products/search?q={escapedQuery}", ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ProductSearchResponse>(cancellationToken: ct);
        logger.LogInformation("Внешний API вернул {Count} товаров по запросу {Query}",
            result?.Products.Count ?? 0, query);

        return result?.Products ?? Array.Empty<ProductDto>();
    }
}
