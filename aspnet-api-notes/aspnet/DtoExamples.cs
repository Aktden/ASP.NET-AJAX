namespace AspNetApiNotes.Dto;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // В конспекте отмечен риск: клиент может прислать "Admin".
    public string Role { get; set; } = string.Empty;
}

// Для PUT/PATCH в конспекте отмечен Optional<T>,
// чтобы различать null и отсутствие значения.

// Request DTO  — вход.
// Response DTO — выход.
// Query DTO    — параметры фильтрации/пагинации.

public record CreateProductRequest(string Name, decimal Price);

// Пример mapper'а из конспекта:
//
// public static ProductResponse ToResponse(this Product p) =>
//     new(p.Id, p.Name, p.Price, p.Category.Name);
//
// AutoMapper:
// CreateMap<Product, ProductResponse>();
