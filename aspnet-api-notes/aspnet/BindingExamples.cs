using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Localization;

namespace AspNetApiNotes.Binding;

public class BindingExamplesController : ControllerBase
{
    [HttpGet("products/{id}")]
    public IActionResult Get(int id) => Ok(id);

    // GET /products/42 -> id = 42

    [HttpPost("{categoryId}/products")]
    public IActionResult Create(
        [FromRoute] int categoryId,
        [FromQuery] bool notify,
        [FromBody] CreateProductRequest request,
        [FromHeader(Name = "X-Request-Id")] string requestId)
    {
        // Логика обработчика зависит от вашего проекта.
        return Ok();
    }

    [HttpGet("by-ids")]
    public IActionResult GetByIds(
        [ModelBinder(typeof(CsvIntArrayBinder))] int[] ids) => Ok(ids);

    public IActionResult ExampleModelState()
    {
        // Если, например, "abc" не удалось преобразовать в int,
        // ошибка попадает в ModelState.
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        return Ok();
    }
}

// Источники binding из конспекта:
//
// Route        [FromRoute]          /products/42
// Query string [FromQuery]          /products?page=2
// Form         [FromForm]           application/x-www-form-urlencoded, multipart/form-data
// Body         [FromBody]           JSON, XML
// Header       [FromHeader]         X-Api-Key: abc
// Services     [FromServices]       DI-контейнер
//              [FromKeyedServices]
//
// [BindRequired]                  — если значение не найдено, ошибка в ModelState
// [ModelBinder(typeof(MyBinder))] — свой binder
// [FromServices]                  — добавляет сервис в action

public class UserDto
{
    public string Name { get; set; } = string.Empty;

    [BindNever] // свойство игнорируется binder'ом
    public bool IsAdmin { get; set; } // клиент не сможет прислать true через binding
}

public class CsvIntArrayBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext ctx)
    {
        var raw = ctx.ValueProvider.GetValue(ctx.ModelName).FirstValue;

        if (string.IsNullOrEmpty(raw))
        {
            ctx.Result = ModelBindingResult.Failed();
            return Task.CompletedTask;
        }

        var arr = raw
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(int.Parse)
            .ToArray();

        ctx.Result = ModelBindingResult.Success(arr);
        return Task.CompletedTask;
    }
}

public record CreateProductRequest(string Name, decimal Price);

public static class BindingServiceRegistration
{
    public static IServiceCollection AddBindingExamples(this IServiceCollection services)
    {
        services.Configure<RequestLocalizationOptions>(o =>
        {
            o.SetDefaultCulture("en-US");
            o.SupportedCultures = new[] { new CultureInfo("en-US") };
        });

        return services;
    }
}
