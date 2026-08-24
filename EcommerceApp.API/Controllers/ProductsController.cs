using EcommerceApp.Application.Products.DTOs;
using EcommerceApp.Application.Products.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApp.API.Controllers;

// Reads (GetAll/GetById) are open to any authenticated user; writes are Admin-only —
// the class-level [Authorize] sets the floor, each write action raises it with Roles = "Admin".
[ApiController]
[Route("api/[controller]")]
public class ProductsController(IProductService service, IValidator<CreateProductDto> validator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? category,
        [FromQuery] string? search,
        [FromQuery] bool? inStockOnly)
    {
        var products = await service.GetAllAsync(category, search, inStockOnly);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await service.GetByIdAsync(id);
        return product is null ? NotFound(new { message = $"Product {id} not found" }) : Ok(product);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage }));

        var created = await service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductDto dto)
    {
        var updated = await service.UpdateAsync(id, dto);
        return updated is null ? NotFound(new { message = $"Product {id} not found" }) : Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await service.DeleteAsync(id);
        return deleted ? NoContent() : NotFound(new { message = $"Product {id} not found" });
    }
}
