using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Carts;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class CartService : ICartService
{
    private readonly IUnitOfWork _uow;

    public CartService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    private static long GetUserId(ClaimsPrincipal principal)
    {
        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(ClaimTypes.Name)?.Value;
        if (!long.TryParse(sub, out var id))
        {
            throw new UnauthorizedAccessException("User id trong token không hợp lệ.");
        }
        return id;
    }

    public async Task<CartResponse> GetCartAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var cart = await EnsureCartAsync(userId, cancellationToken);

        var items = await _uow.CartItems.Query()
            .Include(i => i.Listing)
            .Where(i => i.CartId == cart.CartId)
            .ToListAsync(cancellationToken);

        var updated = false;
        foreach (var item in items)
        {
            var isAvailable = item.Listing != null &&
                              string.Equals(item.Listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase) &&
                              (item.Listing.AvailableQuantity ?? 0) > 0;
            var nextStatus = isAvailable ? CartItemStatuses.Available : CartItemStatuses.Unavailable;
            if (!string.Equals(item.Status, nextStatus, StringComparison.OrdinalIgnoreCase))
            {
                item.Status = nextStatus;
                item.UpdatedAt = DateTime.UtcNow;
                _uow.CartItems.Update(item);
                updated = true;
            }
        }

        if (updated)
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }

        var groups = items
            .GroupBy(i => i.SellerId ?? 0)
            .Select(g => new CartSellerGroupResponse(
                g.Key,
                g.Select(MapCartItem).ToList()))
            .ToList();

        return new CartResponse(
            cart.CartId,
            cart.Status,
            cart.CreatedAt,
            cart.UpdatedAt,
            groups);
    }

    public async Task<CartItemResponse> AddItemAsync(ClaimsPrincipal userPrincipal, AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateAddCartItem(request));

        var userId = GetUserId(userPrincipal);
        var listing = await _uow.Listings.Query()
            .FirstOrDefaultAsync(l => l.ListingId == request.ListingId, cancellationToken);
        if (listing == null) throw new InvalidOperationException("Không tìm thấy bài đăng.");
        if (!string.Equals(listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Bài đăng không khả dụng.");
        }
        var availableQuantity = listing.AvailableQuantity ?? 0;
        if (availableQuantity <= 0)
        {
            throw new InvalidOperationException("Bài đăng đã hết hàng.");
        }
        if (!listing.SellerId.HasValue)
        {
            throw new InvalidOperationException("Không tìm thấy người bán của bài đăng.");
        }

        var cart = await EnsureCartAsync(userId, cancellationToken);

        var existing = await _uow.CartItems.Query()
            .FirstOrDefaultAsync(i =>
                i.CartId == cart.CartId &&
                i.ListingId == request.ListingId &&
                i.VariantId == request.VariantId, cancellationToken);

        if (existing != null)
        {
            return MapCartItem(existing);
        }

        var priceSnapshot = listing.Price;
        var currency = "VND";
        var variantSnapshot = request.VariantSnapshot.HasValue ? request.VariantSnapshot.Value.GetRawText() : null;

        var quantity = 1;
        var item = new CartItem
        {
            CartId = cart.CartId,
            ListingId = listing.ListingId,
            SellerId = listing.SellerId,
            VariantId = request.VariantId,
            Quantity = quantity,
            PriceSnapshot = priceSnapshot,
            OriginalPrice = priceSnapshot,
            Currency = currency,
            VariantSnapshot = variantSnapshot,
            Note = request.Note,
            IsSelected = true,
            Status = CartItemStatuses.Available,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _uow.CartItems.AddAsync(item, cancellationToken);
        cart.UpdatedAt = DateTime.UtcNow;
        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync(cancellationToken);

        return MapCartItem(item);
    }

    public async Task<BasicResponse> UpdateItemAsync(ClaimsPrincipal userPrincipal, long cartItemId, UpdateCartItemRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateUpdateCartItem(request));
        var userId = GetUserId(userPrincipal);
        var item = await _uow.CartItems.Query()
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.CartItemId == cartItemId, cancellationToken);
        if (item == null || item.Cart?.UserId != userId)
        {
            return new BasicResponse(false, "Không tìm thấy sản phẩm trong giỏ.");
        }

        if (request.Quantity.HasValue)
        {
            if (request.Quantity.Value <= 0)
            {
                return new BasicResponse(false, "Số lượng phải lớn hơn 0.");
            }

            var listing = await _uow.Listings.Query()
                .FirstOrDefaultAsync(l => l.ListingId == item.ListingId, cancellationToken);
            if (listing == null || !string.Equals(listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
            {
                return new BasicResponse(false, "Bài đăng không khả dụng.");
            }
            var availableQuantity = listing.AvailableQuantity ?? 0;
            if (availableQuantity <= 0)
            {
                return new BasicResponse(false, "Bài đăng đã hết hàng.");
            }

            if (request.Quantity.Value > 1)
            {
                return new BasicResponse(false, "Bài đăng này chỉ còn 1 sản phẩm.");
            }

            item.Quantity = request.Quantity.Value;
        }

        if (request.Note != null)
        {
            item.Note = request.Note;
        }

        if (request.IsSelected.HasValue)
        {
            item.IsSelected = request.IsSelected.Value;
        }

        item.UpdatedAt = DateTime.UtcNow;
        _uow.CartItems.Update(item);
        if (item.Cart != null)
        {
            item.Cart.UpdatedAt = DateTime.UtcNow;
            _uow.Carts.Update(item.Cart);
        }
        await _uow.SaveChangesAsync(cancellationToken);

        return new BasicResponse(true, "Đã cập nhật sản phẩm trong giỏ.");
    }

    public async Task<BasicResponse> RemoveItemAsync(ClaimsPrincipal userPrincipal, long cartItemId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var item = await _uow.CartItems.Query()
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i => i.CartItemId == cartItemId, cancellationToken);
        if (item == null || item.Cart?.UserId != userId)
        {
            return new BasicResponse(false, "Không tìm thấy sản phẩm trong giỏ.");
        }

        _uow.CartItems.Remove(item);
        if (item.Cart != null)
        {
            item.Cart.UpdatedAt = DateTime.UtcNow;
            _uow.Carts.Update(item.Cart);
        }
        await _uow.SaveChangesAsync(cancellationToken);
        return new BasicResponse(true, "Đã xóa sản phẩm khỏi giỏ.");
    }

    public async Task<BasicResponse> ClearAsync(ClaimsPrincipal userPrincipal, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var cart = await _uow.Carts.Query()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatuses.Active, cancellationToken);
        if (cart == null)
        {
            return new BasicResponse(true, "Đã xóa giỏ hàng.");
        }

        var items = await _uow.CartItems.Query()
            .Where(i => i.CartId == cart.CartId)
            .ToListAsync(cancellationToken);

        if (items.Count > 0)
        {
            _uow.CartItems.RemoveRange(items);
            await _uow.SaveChangesAsync(cancellationToken);
        }

        cart.UpdatedAt = DateTime.UtcNow;
        _uow.Carts.Update(cart);
        await _uow.SaveChangesAsync(cancellationToken);

        return new BasicResponse(true, "Đã xóa giỏ hàng.");
    }

    public async Task<CheckoutCartResponse> CheckoutAsync(ClaimsPrincipal userPrincipal, CheckoutCartRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateCheckoutCart(request));
        var userId = GetUserId(userPrincipal);
        if (!PaymentMethods.All.Contains(request.PaymentMethod, StringComparer.OrdinalIgnoreCase))
        {
            return new CheckoutCartResponse(false, "Phương thức thanh toán không hợp lệ.", Array.Empty<CartValidationError>(), Array.Empty<CheckoutOrderSummary>());
        }

        var cart = await _uow.Carts.Query()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatuses.Active, cancellationToken);
        if (cart == null)
        {
            return new CheckoutCartResponse(false, "Cart is empty.", Array.Empty<CartValidationError>(), Array.Empty<CheckoutOrderSummary>());
        }

        var items = await _uow.CartItems.Query()
            .Include(i => i.Listing)
            .Where(i => i.CartId == cart.CartId && (i.IsSelected == null || i.IsSelected == true))
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            return new CheckoutCartResponse(false, "No items selected for checkout.", Array.Empty<CartValidationError>(), Array.Empty<CheckoutOrderSummary>());
        }

        var errors = new List<CartValidationError>();
        foreach (var item in items)
        {
            if (item.Listing == null)
            {
                errors.Add(new CartValidationError(item.CartItemId, "Không tìm thấy bài đăng."));
                continue;
            }
            if (!string.Equals(item.Listing.Status, ListingStatuses.Active, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add(new CartValidationError(item.CartItemId, "Bài đăng không khả dụng."));
                continue;
            }
            if ((item.Listing.AvailableQuantity ?? 0) <= 0)
            {
                errors.Add(new CartValidationError(item.CartItemId, "Bài đăng đã hết hàng."));
                continue;
            }
            if (!item.Listing.SellerId.HasValue)
            {
                errors.Add(new CartValidationError(item.CartItemId, "Không tìm thấy người bán của bài đăng."));
                continue;
            }
            if ((item.Quantity ?? 0) != 1)
            {
                errors.Add(new CartValidationError(item.CartItemId, "Bài đăng này chỉ còn 1 sản phẩm."));
                continue;
            }

            var hasActiveOrder = await _uow.Orders.Query()
                .AnyAsync(o =>
                    o.OrderItems.Any(oi => oi.ListingId == item.ListingId) &&
                    (o.Status == OrderStatuses.Pending ||
                     o.Status == OrderStatuses.Confirmed ||
                     o.Status == OrderStatuses.Delivering ||
                     o.Status == OrderStatuses.Delivered ||
                     o.Status == OrderStatuses.Completed ||
                     o.Status == OrderStatuses.Disputed), cancellationToken);
            if (hasActiveOrder)
            {
                errors.Add(new CartValidationError(item.CartItemId, "Bài đăng đã có đơn hàng đang hoạt động."));
            }
        }

        if (errors.Count > 0)
        {
            return new CheckoutCartResponse(false, "Giỏ hàng có sản phẩm không hợp lệ.", errors, Array.Empty<CheckoutOrderSummary>());
        }

        var orderSummaries = new List<CheckoutOrderSummary>();
        await using var tx = await _uow.BeginTransactionAsync(cancellationToken);
        try
        {
            var groups = items.GroupBy(i => i.SellerId!.Value);
            foreach (var group in groups)
            {
                var groupItems = group.ToList();
                var total = groupItems.Sum(i => i.PriceSnapshot ?? 0);
                var listingIds = groupItems.Select(i => i.ListingId ?? 0).Where(id => id > 0).ToList();

                var order = new Order
                {
                    BuyerId = userId,
                    SellerId = group.Key,
                    ListingId = listingIds.Count == 1 ? listingIds[0] : null,
                    TotalAmount = total,
                    PaymentMethod = request.PaymentMethod,
                    Status = OrderStatuses.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                await _uow.Orders.AddAsync(order, cancellationToken);
                await _uow.SaveChangesAsync(cancellationToken);

                foreach (var item in groupItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderId = order.OrderId,
                        ListingId = item.ListingId,
                        Price = item.PriceSnapshot
                    };
                    await _uow.OrderItems.AddAsync(orderItem, cancellationToken);

                    if (item.Listing != null)
                    {
                        item.Listing.Status = ListingStatuses.Reserved;
                        item.Listing.AvailableQuantity = 0;
                        item.Listing.UpdatedAt = DateTime.UtcNow;
                        _uow.Listings.Update(item.Listing);
                    }
                }
                await _uow.SaveChangesAsync(cancellationToken);

                orderSummaries.Add(new CheckoutOrderSummary(
                    order.OrderId,
                    group.Key,
                    order.TotalAmount,
                    order.PaymentMethod,
                    order.Status,
                    listingIds));
            }

            _uow.CartItems.RemoveRange(items);
            cart.UpdatedAt = DateTime.UtcNow;
            _uow.Carts.Update(cart);
            await _uow.SaveChangesAsync(cancellationToken);

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        return new CheckoutCartResponse(true, "Checkout created orders.", Array.Empty<CartValidationError>(), orderSummaries);
    }

    private async Task<Cart> EnsureCartAsync(long userId, CancellationToken cancellationToken)
    {
        var cart = await _uow.Carts.Query()
            .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == CartStatuses.Active, cancellationToken);
        if (cart != null) return cart;

        cart = new Cart
        {
            UserId = userId,
            Status = CartStatuses.Active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _uow.Carts.AddAsync(cart, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
        return cart;
    }

    private static CartItemResponse MapCartItem(CartItem item)
    {
        return new CartItemResponse(
            item.CartItemId,
            item.ListingId ?? 0,
            item.SellerId ?? 0,
            item.VariantId,
            item.Quantity ?? 0,
            item.PriceSnapshot,
            item.OriginalPrice,
            item.Currency,
            item.VariantSnapshot,
            item.Note,
            item.IsSelected ?? false,
            item.Status,
            item.CreatedAt,
            item.UpdatedAt);
    }

}










