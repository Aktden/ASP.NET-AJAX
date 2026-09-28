using Microsoft.AspNetCore.Mvc;

namespace AspNetApiNotes.Errors;

public class ProblemDetailsExamplesController : ControllerBase
{
    public IActionResult NotFoundExample(int id)
    {
        return Problem(
            title: "Product not found",
            detail: $"Product with id {id} does not exist",
            statusCode: StatusCodes.Status404NotFound,
            type: "https://example.com/errors/not-found");
    }

    public IActionResult ConflictExample()
    {
        var pd = new ProblemDetails
        {
            Status = 409,
            Title = "Conflict",
            Detail = "Product with this name already exists",
            Instance = HttpContext.Request.Path
        };

        pd.Extensions["traceId"] = HttpContext.TraceIdentifier;

        return new ObjectResult(pd) { StatusCode = 409 };
    }

    public IActionResult ValidationExample()
    {
        ModelState.AddModelError("Name", "Имя уже занято");
        return ValidationProblem(ModelState);
    }
}

/*
Пример ProblemDetails из конспекта:

{
  "type": "https://example.com/errors/not-found",
  "title": "Product not found",
  "status": 404,
  "detail": "Product with id 42 does not exist",
  "instance": "/api/products/42"
}

Поля:
type       — URI типа ошибки
title      — краткое описание
status     — HTTP-статус
detail     — детали конкретного случая
instance   — URI конкретного запроса
extensions — дополнительные поля: traceId, timestamp, errors и т.д.

Пример ошибки валидации:

{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": ["Имя обязательно"],
    "Price": ["Price must be between 0.01 and 1000000"]
  },
  "traceId": "00-abc..."
}
*/
