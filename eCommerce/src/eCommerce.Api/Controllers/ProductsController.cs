using eCommerce.Application.Catalog.Products.Commands.CreateProduct;
using eCommerce.Application.Catalog.Products.Commands.DeactivateProduct;
using eCommerce.Application.Catalog.Products.Commands.RestockProduct;
using eCommerce.Application.Catalog.Products.Commands.UpdateProductPrice;
using eCommerce.Application.Catalog.Products.Dtos;
using eCommerce.Application.Catalog.Products.Queries.GetProductById;
using eCommerce.Application.Catalog.Products.Queries.GetProducts;
using eCommerce.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace eCommerce.Api.Controllers;

/// <summary>
/// Thin transport layer: bind, dispatch, translate the result to a status code.
/// Business rules live in the Application and Domain layers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    public ProductsController(ISender sender) => _sender = sender;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedList<ProductDto>>> GetProducts(
        [FromQuery] GetProductsQuery query,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(query, cancellationToken));

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetProductById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await _sender.Send(new GetProductByIdQuery(id), cancellationToken));

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateProduct(
        CreateProductCommand command,
        CancellationToken cancellationToken)
    {
        var id = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetProductById), new { id }, id);
    }

    [HttpPut("{id:guid}/price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrice(
        Guid id,
        UpdateProductPriceRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(
            new UpdateProductPriceCommand(id, request.Price, request.Currency),
            cancellationToken);

        return NoContent();
    }

    [HttpPost("{id:guid}/restock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Restock(
        Guid id,
        RestockProductRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.Send(new RestockProductCommand(id, request.Quantity), cancellationToken);

        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeactivateProductCommand(id), cancellationToken);

        return NoContent();
    }
}

/// <summary>Body of a price update; the product id comes from the route.</summary>
public sealed record UpdateProductPriceRequest(decimal Price, string Currency);

/// <summary>Body of a restock request; the product id comes from the route.</summary>
public sealed record RestockProductRequest(int Quantity);
