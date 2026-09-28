using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace AspNetApiNotes.Validation;

public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(
        @"^(?=.*[A-Za-z])(?=.*\d).+$",
        ErrorMessage = "Нужны буквы и цифры")]
    public string Password { get; set; } = "";

    [Required, Compare(nameof(Password))]
    public string ConfirmPassword { get; set; } = "";

    [Range(18, 120)]
    public int Age { get; set; }
}

public class NotInFutureAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext ctx)
    {
        if (value is DateTime dt && dt > DateTime.UtcNow)
        {
            return new ValidationResult(
                ErrorMessage ?? "Дата не может быть в будущем",
                new[] { ctx.MemberName! });
        }

        return ValidationResult.Success;
    }
}

public class EventRequest
{
    [NotInFuture]
    public DateTime Date { get; set; }
}

public class DateRangeRequest : IValidatableObject
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext ctx)
    {
        if (To <= From)
        {
            yield return new ValidationResult(
                "To должно быть больше From",
                new[] { nameof(To) });
        }

        if ((To - From).TotalDays > 365)
        {
            yield return new ValidationResult(
                "Диапазон не должен превышать год",
                new[] { nameof(From), nameof(To) });
        }
    }
}

public class OrderRequest
{
    [Required]
    public CustomerDto Customer { get; set; } = new();

    [MinLength(1)]
    public List<OrderItemDto> Items { get; set; } = new();
}

// Заглушки только для сохранения формы примера из конспекта.
public class CustomerDto { }
public class OrderItemDto { }

// В конспекте CreateProductValidator использует CategoryId.
// Поэтому здесь DTO расширен CategoryId, чтобы пример был самодостаточным.
public record CreateProductRequest(string Name, decimal Price, int CategoryId);

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Имя обязательно")
            .MinimumLength(3)
            .MaximumLength(100);

        RuleFor(x => x.Price)
            .GreaterThan(0)
            .LessThan(1_000_000);

        RuleFor(x => x.CategoryId)
            .GreaterThan(0);

        When(x => x.Price > 10000, () =>
        {
            RuleFor(x => x.Name)
                .MinimumLength(10)
                .WithMessage("Дорогие товары требуют длинного названия");
        });
    }
}

/*
DataAnnotations из конспекта:

[Required]                               — значение обязательно
[StringLength(max, MinimumLength = min)] — длина строки
[MinLength], [MaxLength]                 — коллекции/строки
[Range(min, max)]                        — числовой/датовый диапазон
[EmailAddress]                           — формат email
[Phone]                                  — формат телефона
[Url]                                    — формат URL
[RegularExpression(pattern)]             — regex
[Compare(otherProp)]                     — совпадение с другим свойством
[CreditCard]                             — алгоритм Луна
[EnumDataType]                           — значение принадлежит enum
[Remote]                                 — AJAX-валидация на клиенте (MVC)
*/
