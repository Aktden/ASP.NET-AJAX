using Microsoft.OpenApi.Models;
using TodoManagementApi.Models;
using TodoManagementApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TodoStore>();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Todo Management API",
        Version = "v1",
        Description = "API для управления задачами. Поддерживает фильтрацию и создание.",
        Contact = new OpenApiContact
        {
            Name = "API Support",
            Email = "support@example.com"
        },
        License = new OpenApiLicense
        {
            Name = "MIT",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Todo Management API v1");
});

app.MapGet("/api/todos", (bool? completed, TodoStore store) =>
    Results.Ok(store.GetAll(completed)))
    .WithName("GetTodos")
    .WithSummary("Get all tasks")
    .WithDescription("Returns all tasks. Use completed=true or completed=false to filter.")
    .WithOpenApi();

app.MapGet("/api/todos/{id:int}", (int id, TodoStore store) =>
{
    var todo = store.GetById(id);

    if (todo is null)
    {
        return (IResult)Results.NotFound(
            new { message = $"Todo with ID {id} was not found." });
    }

    return Results.Ok(todo);
})
.WithName("GetTodoById")
.WithSummary("Get a task by ID")
.WithOpenApi();

app.MapPost("/api/todos", (CreateTodoRequest request, TodoStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Title))
    {
        return (IResult)Results.BadRequest(
            new { message = "Title must not be empty." });
    }

    var created = store.Add(request.Title);
    return Results.Created($"/api/todos/{created.Id}", created);
})
.WithName("CreateTodo")
.WithSummary("Create a task")
.WithOpenApi();

app.Run();
