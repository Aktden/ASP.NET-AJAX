using Microsoft.AspNetCore.Mvc;
using Shop.Api.Contracts;
using Shop.Api.Extensions;
using Shop.Application.Orders.CancelOrder;
using Shop.Application.Orders.CreateOrder;
using Shop.Application.Orders.GetOrder;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersApiController : ControllerBase
{
    private readonly CreateOrderUseCase _createOrder;
    private readonly GetOrderUseCase _getOrder;
    private readonly CancelOrderUseCase _cancelOrder;

    public OrdersApiController(
        CreateOrderUseCase createOrder,
        GetOrderUseCase getOrder,
        CancelOrderUseCase cancelOrder)
    {
        _createOrder = createOrder;
        _getOrder = getOrder;
        _cancelOrder = cancelOrder;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        // 1. DTO → Command
        var command = new CreateOrderCommand(
            request.CustomerEmail,
            request.Items.Select(i => new CreateOrderItem(i.ProductId, i.Quantity)).ToList());

        // 2. Use Case
        var result = await _createOrder.ExecuteAsync(command, ct);

        // 3. Result → IActionResult (201 Created + Location)
        return result.ToApiResult(this, id =>
            CreatedAtAction(nameof(GetById), new { id }, new { id }));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _getOrder.ExecuteAsync(new GetOrderQuery(id), ct);

        return result.ToApiResult(this, dto =>
            Ok(new OrderResponse(
                dto.Id,
                dto.CustomerEmail,
                dto.Total,
                dto.Items.Select(i => new OrderItemResponse(i.ProductId, i.Name, i.Price, i.Quantity)).ToList())));
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var result = await _cancelOrder.ExecuteAsync(new CancelOrderCommand(id), ct);

        return result.ToApiResult(this, _ => NoContent());
    }
}
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();          // MVC (Views)
builder.Services.AddControllers();                    // REST (без views) — либо совместить:
// Проще: builder.Services.AddControllersWithViews(); — покрывает оба.

builder.Services.AddDbContext<AppDbContext>(o => o.UseSqlite("Data Source=shop.db"));
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddScoped<CreateOrderUseCase>();
builder.Services.AddScoped<GetOrderUseCase>();
builder.Services.AddScoped<CancelOrderUseCase>();

// ProblemDetails как стандарт ошибок
builder.Services.AddProblemDetails();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

// MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// REST
app.MapControllers();

app.Run();