namespace ProductService.Controllers;

using Microsoft.AspNetCore.Mvc;
using ProductService.Models;
using ProductService.Messaging;
using Svc = ProductService.Services.ProductService;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly Svc _service;
    private readonly IMessageBus _bus;

    public ProductsController(Svc service, IMessageBus bus)
    {
        _service = service;
        _bus = bus;
    }

    [HttpGet]
    public async Task<ActionResult<List<Product>>> GetAll([FromQuery] string? category, [FromQuery] bool? active)
    {
        var products = await _service.GetAllAsync(category, active);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetById(Guid id)
    {
        var product = await _service.GetByIdAsync(id);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpGet("sku/{sku}")]
    public async Task<ActionResult<Product>> GetBySku(string sku)
    {
        var product = await _service.GetBySkuAsync(sku);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> Create([FromBody] CreateProductRequest request)
    {
        var product = await _service.CreateAsync(request);
        await _bus.PublishAsync(new ProductEvent("created", product.Id, product.Sku, new { product.Name, product.Price }, DateTime.UtcNow));
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Product>> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await _service.UpdateAsync(id, request);
        if (product is null) return NotFound();
        await _bus.PublishAsync(new ProductEvent("updated", product.Id, product.Sku, request, DateTime.UtcNow));
        return Ok(product);
    }

    [HttpPost("{id:guid}/stock")]
    public async Task<ActionResult<Product>> UpdateStock(Guid id, [FromBody] StockUpdateRequest request)
    {
        try
        {
            var product = await _service.UpdateStockAsync(id, request.Quantity);
            if (product is null) return NotFound();
            await _bus.PublishAsync(new ProductEvent("stock_updated", product.Id, product.Sku, new { request.Quantity, request.Reason, NewStock = product.Stock }, DateTime.UtcNow));

            if (product.Stock <= 10)
                await _bus.PublishAsync(new ProductEvent("low_stock", product.Id, product.Sku, new { product.Stock }, DateTime.UtcNow));

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return UnprocessableEntity(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _service.DeleteAsync(id);
        if (!deleted) return NotFound();
        await _bus.PublishAsync(new ProductEvent("deactivated", id, "", null, DateTime.UtcNow));
        return NoContent();
    }
}

[ApiController]
[Route("api/[controller]")]
public class MetaController : ControllerBase
{
    [HttpGet("/info")]
    public IActionResult Info() => Ok(new
    {
        service = "product-service",
        version = "1.0.0",
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
        timestamp = DateTime.UtcNow,
    });
}
