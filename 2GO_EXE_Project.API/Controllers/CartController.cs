using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using _2GO_EXE_Project.BAL.DTOs.Carts;
using _2GO_EXE_Project.BAL.Interfaces;

namespace _2GO_EXE_Project.API.Controllers;

[ApiController]
[Route("api/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ICartService _cartService;

    public CartController(ICartService cartService)
    {
        _cartService = cartService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken = default)
    {
        var result = await _cartService.GetCartAsync(User, cancellationToken);
        return Ok(result);
    }

    [HttpPost("items")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _cartService.AddItemAsync(User, request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("items/{cartItemId:long}")]
    public async Task<IActionResult> UpdateItem(long cartItemId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _cartService.UpdateItemAsync(User, cartItemId, request, cancellationToken);
        if (!result.Success) return BadRequest(result.Message);
        return Ok(result);
    }

    [HttpDelete("items/{cartItemId:long}")]
    public async Task<IActionResult> RemoveItem(long cartItemId, CancellationToken cancellationToken = default)
    {
        var result = await _cartService.RemoveItemAsync(User, cartItemId, cancellationToken);
        if (!result.Success) return NotFound(result.Message);
        return Ok(result);
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken = default)
    {
        var result = await _cartService.ClearAsync(User, cancellationToken);
        return Ok(result);
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CheckoutCartRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _cartService.CheckoutAsync(User, request, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }
}
