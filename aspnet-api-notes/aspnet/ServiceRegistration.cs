using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using AspNetApiNotes.Errors;
using AspNetApiNotes.Validation;

namespace AspNetApiNotes;

public static class ServiceRegistration
{
    public static void RegisterServices(WebApplicationBuilder builder)
    {
        // FluentValidation
        builder.Services.AddValidatorsFromAssemblyContaining<CreateProductValidator>();
        builder.Services.AddFluentValidationAutoValidation();

        // ProblemDetails
        builder.Services.AddProblemDetails();

        // Кастомная ProblemDetailsFactory
        builder.Services.AddSingleton<ProblemDetailsFactory, CustomProblemDetailsFactory>();

        // Глобальная обработка ошибок
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
    }

    public static void ConfigurePipeline(WebApplication app)
    {
        app.UseExceptionHandler();
    }
}
