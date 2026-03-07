using System.Security.Claims;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Carts;

namespace _2GO_EXE_Project.BAL.Interfaces;

public interface ICartService
{
    Task<CartResponse> GetCartAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default);
    Task<CartItemResponse> AddItemAsync(ClaimsPrincipal userPrincipal, AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> UpdateItemAsync(ClaimsPrincipal userPrincipal, long cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default);
    Task<BasicResponse> RemoveItemAsync(ClaimsPrincipal userPrincipal, long cartItemId, CancellationToken cancellationToken = default);
    Task<BasicResponse> ClearAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default);
    Task<CheckoutCartResponse> CheckoutAsync(ClaimsPrincipal userPrincipal, CheckoutCartRequest request, CancellationToken cancellationToken = default);
}
