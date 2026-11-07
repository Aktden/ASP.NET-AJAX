namespace PracticalWork_3Topics.Dtos;

public sealed record ProductDto(int Id, string Title, string Description, double Price);

public sealed record ProductSearchResponse(
    List<ProductDto> Products,
    int Total,
    int Skip,
    int Limit);
