using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Shipping;
using _2GO_EXE_Project.BAL.DTOs.Notifications;
using _2GO_EXE_Project.BAL.Interfaces;
using _2GO_EXE_Project.BAL.Settings;
using _2GO_EXE_Project.DAL.Entities;
using _2GO_EXE_Project.DAL.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using _2GO_EXE_Project.BAL.Validation;

namespace _2GO_EXE_Project.BAL.Services;

public class ShippingService : IShippingService
{
    private readonly IUnitOfWork _uow;
    private readonly IGhnShippingService _ghnShippingService;
    private readonly GhnSettings _ghnSettings;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ShippingService> _logger;

    public ShippingService(IUnitOfWork uow, IGhnShippingService ghnShippingService, IOptions<GhnSettings> options, INotificationService notificationService, ILogger<ShippingService> logger)
    {
        _uow = uow;
        _ghnShippingService = ghnShippingService;
        _ghnSettings = options.Value ?? new GhnSettings();
        _notificationService = notificationService;
        _logger = logger;
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

    public async Task<ShippingResponse> CreateAsync(ClaimsPrincipal userPrincipal, CreateShippingRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateCreateShipping(request));
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.Query()
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
        if (order == null) throw new InvalidOperationException("Không tìm thấy don hàng.");
        if (order.SellerId != userId) throw new InvalidOperationException("Chỉ người bán mới có thể tạo vận chuyển.");

        var existing = await _uow.ShippingRequests.Query()
            .FirstOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);
        if (existing != null)
        {
            var updated = false;
            var notify = false;
            if (string.IsNullOrWhiteSpace(existing.Provider) && !string.IsNullOrWhiteSpace(request.Provider))
            {
                existing.Provider = request.Provider;
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(existing.PickupAddress) && !string.IsNullOrWhiteSpace(request.PickupAddress))
            {
                existing.PickupAddress = request.PickupAddress;
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(existing.DeliveryAddress))
            {
                if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
                {
                    throw new InvalidOperationException("Địa chỉ giao hàng là bắt buộc.");
                }
                existing.DeliveryAddress = request.DeliveryAddress;
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(existing.Status))
            {
                existing.Status = ShippingStatuses.Requested;
                updated = true;
                notify = true;
            }
            if (updated)
            {
                _uow.ShippingRequests.Update(existing);
                await _uow.SaveChangesAsync(cancellationToken);
                if (notify)
                {
                    await UpdateOrderStatusForShippingAsync(existing, ShippingStatuses.Requested, cancellationToken);
                    await NotifyShippingStatusAsync(order, ShippingStatuses.Requested, cancellationToken);
                }
            }
            return MapShippingResponse(existing);
        }

        if (!string.Equals(order.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ có thể tạo vận chuyển khi đơn hàng ở trạng thái Confirmed.");
        }

        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            throw new InvalidOperationException("Địa chỉ giao hàng là bắt buộc.");
        }

        var ship = new ShippingRequest
        {
            OrderId = request.OrderId,
            Provider = request.Provider,
            PickupAddress = request.PickupAddress,
            DeliveryAddress = request.DeliveryAddress,
            Status = ShippingStatuses.Requested,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.ShippingRequests.AddAsync(ship, cancellationToken);
        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Shipping request already exists for order {OrderId}. Returning existing record.", request.OrderId);
            // In case of a concurrent request, return the existing shipping record
            var concurrent = await _uow.ShippingRequests.Query()
                .FirstOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);
            if (concurrent != null)
            {
                return MapShippingResponse(concurrent);
            }
            throw;
        }
        await UpdateOrderStatusForShippingAsync(ship, ShippingStatuses.Requested, cancellationToken);
        await NotifyShippingStatusAsync(order, ShippingStatuses.Requested, cancellationToken);

        return MapShippingResponse(ship);
    }

    public async Task<ShippingResponse> CreateGhnAsync(ClaimsPrincipal userPrincipal, CreateGhnShippingRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateCreateGhnShipping(request));
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.Query()
            .FirstOrDefaultAsync(o => o.OrderId == request.OrderId, cancellationToken);
        if (order == null) throw new InvalidOperationException("Không tìm thấy don hàng.");
        if (order.SellerId != userId) throw new InvalidOperationException("Chỉ người bán mới có thể tạo vận chuyển.");

        var existing = await _uow.ShippingRequests.Query()
            .FirstOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);
        if (existing != null && !string.IsNullOrWhiteSpace(existing.TrackingCode))
        {
            if (!string.IsNullOrWhiteSpace(request.ToPhone) &&
                !string.Equals(order.DeliveryPhone, request.ToPhone, StringComparison.Ordinal))
            {
                order.DeliveryPhone = request.ToPhone.Trim();
                _uow.Orders.Update(order);
                await _uow.SaveChangesAsync(cancellationToken);
            }
            return MapShippingResponse(existing);
        }

        if (!string.Equals(order.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Chỉ có thể tạo vận chuyển khi đơn hàng ở trạng thái Confirmed.");
        }

        var orderCode = await _ghnShippingService.CreateOrderAsync(request, cancellationToken);

        if (existing != null)
        {
            var updated = false;
            if (string.IsNullOrWhiteSpace(existing.Provider))
            {
                existing.Provider = "GHN";
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(existing.TrackingCode) && !string.IsNullOrWhiteSpace(orderCode))
            {
                existing.TrackingCode = orderCode;
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(existing.PickupAddress) && !string.IsNullOrWhiteSpace(request.FromAddress))
            {
                existing.PickupAddress = request.FromAddress;
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(existing.DeliveryAddress) && !string.IsNullOrWhiteSpace(request.ToAddress))
            {
                existing.DeliveryAddress = request.ToAddress;
                updated = true;
            }
            if (string.IsNullOrWhiteSpace(existing.Status))
            {
                existing.Status = ShippingStatuses.Requested;
                updated = true;
            }
            if (updated)
            {
                _uow.ShippingRequests.Update(existing);
                if (!string.IsNullOrWhiteSpace(request.ToPhone) &&
                    !string.Equals(order.DeliveryPhone, request.ToPhone, StringComparison.Ordinal))
                {
                    order.DeliveryPhone = request.ToPhone.Trim();
                    _uow.Orders.Update(order);
                }
                await _uow.SaveChangesAsync(cancellationToken);
            }
            else if (!string.IsNullOrWhiteSpace(request.ToPhone) &&
                     !string.Equals(order.DeliveryPhone, request.ToPhone, StringComparison.Ordinal))
            {
                order.DeliveryPhone = request.ToPhone.Trim();
                _uow.Orders.Update(order);
                await _uow.SaveChangesAsync(cancellationToken);
            }
            await UpdateOrderStatusForShippingAsync(existing, ShippingStatuses.Requested, cancellationToken);
            await NotifyShippingStatusAsync(order, ShippingStatuses.Requested, cancellationToken);
            return MapShippingResponse(existing);
        }

        var ship = new ShippingRequest
        {
            OrderId = request.OrderId,
            Provider = "GHN",
            TrackingCode = string.IsNullOrWhiteSpace(orderCode) ? null : orderCode,
            PickupAddress = request.FromAddress,
            DeliveryAddress = request.ToAddress,
            Status = ShippingStatuses.Requested,
            CreatedAt = DateTime.UtcNow
        };

        await _uow.ShippingRequests.AddAsync(ship, cancellationToken);
        try
        {
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "GHN shipping request already exists for order {OrderId}. Returning existing record.", request.OrderId);
            // In case of a concurrent request, return the existing shipping record
            var concurrent = await _uow.ShippingRequests.Query()
                .FirstOrDefaultAsync(s => s.OrderId == request.OrderId, cancellationToken);
            if (concurrent != null)
            {
                return MapShippingResponse(concurrent);
            }
            throw;
        }
        if (!string.IsNullOrWhiteSpace(request.ToPhone))
        {
            order.DeliveryPhone = request.ToPhone.Trim();
            _uow.Orders.Update(order);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        await UpdateOrderStatusForShippingAsync(ship, ShippingStatuses.Requested, cancellationToken);
        await NotifyShippingStatusAsync(order, ShippingStatuses.Requested, cancellationToken);

        return MapShippingResponse(ship);
    }

    public async Task<IReadOnlyList<GhnProvinceResponse>> GetGhnProvincesAsync(CancellationToken cancellationToken = default)
    {
        return await _ghnShippingService.GetProvincesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<GhnDistrictResponse>> GetGhnDistrictsAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        if (provinceId <= 0) return Array.Empty<GhnDistrictResponse>();
        return await _ghnShippingService.GetDistrictsAsync(provinceId, cancellationToken);
    }

    public async Task<IReadOnlyList<GhnWardResponse>> GetGhnWardsAsync(int districtId, CancellationToken cancellationToken = default)
    {
        if (districtId <= 0) return Array.Empty<GhnWardResponse>();
        return await _ghnShippingService.GetWardsAsync(districtId, cancellationToken);
    }

    public async Task<GhnFeeResponse> GetGhnFeeAsync(GhnFeeRequest request, CancellationToken cancellationToken = default)
    {
        var feeValidation = RequestValidator.ValidateGhnFee(request);
        if (!feeValidation.IsValid)
        {
            return new GhnFeeResponse(0, 0, 0, 0, 0, 0);
        }
        if (request.FromDistrictId <= 0 || request.ToDistrictId <= 0)
        {
            return new GhnFeeResponse(0, 0, 0, 0, 0, 0);
        }
        if (string.IsNullOrWhiteSpace(request.FromWardCode) || string.IsNullOrWhiteSpace(request.ToWardCode))
        {
            return new GhnFeeResponse(0, 0, 0, 0, 0, 0);
        }
        return await _ghnShippingService.GetFeeAsync(request, cancellationToken);
    }

    public async Task<GhnCancelResponse> CancelGhnAsync(ClaimsPrincipal userPrincipal, GhnCancelRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateGhnCancel(request));
        var userId = GetUserId(userPrincipal);
        if (request.OrderCodes == null || request.OrderCodes.Count == 0)
        {
            return new GhnCancelResponse(Array.Empty<GhnCancelResult>());
        }

        var codes = request.OrderCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (codes.Length == 0)
        {
            return new GhnCancelResponse(Array.Empty<GhnCancelResult>());
        }

        var shippingList = await _uow.ShippingRequests.Query()
            .Where(s => s.TrackingCode != null && codes.Contains(s.TrackingCode))
            .ToListAsync(cancellationToken);
        if (shippingList.Count == 0)
        {
            throw new InvalidOperationException("Không tìm thấy vận chuyển với GHN codes đã cung cấp.");
        }

        foreach (var ship in shippingList)
        {
            var order = await _uow.Orders.GetByIdAsync(ship.OrderId ?? 0);
            if (order == null || order.SellerId != userId)
            {
                throw new InvalidOperationException("Không được phép hủy vận chuyển này.");
            }
        }

        var response = await _ghnShippingService.CancelAsync(new GhnCancelRequest(codes), cancellationToken);
        foreach (var result in response.Results)
        {
            if (!result.Result || string.IsNullOrWhiteSpace(result.OrderCode)) continue;
            var ship = shippingList.FirstOrDefault(s => string.Equals(s.TrackingCode, result.OrderCode, StringComparison.OrdinalIgnoreCase));
            if (ship == null) continue;
            ship.Status = ShippingStatuses.Failed;
            _uow.ShippingRequests.Update(ship);
        }
        await _uow.SaveChangesAsync(cancellationToken);
        foreach (var ship in shippingList)
        {
            var order = await _uow.Orders.GetByIdAsync(ship.OrderId ?? 0);
            if (order != null)
            {
                await NotifyShippingStatusAsync(order, ShippingStatuses.Failed, cancellationToken);
            }
        }
        return response;
    }

    public async Task<GhnPrintTokenResponse> GetGhnPrintTokenAsync(ClaimsPrincipal userPrincipal, GhnPrintTokenRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateGhnPrintToken(request));
        var userId = GetUserId(userPrincipal);
        if (request.OrderCodes == null || request.OrderCodes.Count == 0)
        {
            return new GhnPrintTokenResponse(string.Empty, string.Empty, string.Empty, string.Empty);
        }

        var codes = request.OrderCodes.Where(c => !string.IsNullOrWhiteSpace(c)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (codes.Length == 0)
        {
            return new GhnPrintTokenResponse(string.Empty, string.Empty, string.Empty, string.Empty);
        }

        var shippingList = await _uow.ShippingRequests.Query()
            .Where(s => s.TrackingCode != null && codes.Contains(s.TrackingCode))
            .ToListAsync(cancellationToken);
        if (shippingList.Count == 0)
        {
            throw new InvalidOperationException("Không tìm thấy vận chuyển với GHN codes đã cung cấp.");
        }

        foreach (var ship in shippingList)
        {
            var order = await _uow.Orders.GetByIdAsync(ship.OrderId ?? 0);
            if (order == null || order.SellerId != userId)
            {
                throw new InvalidOperationException("Không được phép truy cập vận chuyển này.");
            }
        }

        var tokenResponse = await _ghnShippingService.GetPrintTokenAsync(new GhnPrintTokenRequest(codes), cancellationToken);
        if (!string.IsNullOrWhiteSpace(tokenResponse.Token))
        {
            foreach (var ship in shippingList)
            {
                ship.LabelA5Url = tokenResponse.PrintA5Url;
                ship.Label80x80Url = tokenResponse.Print80x80Url;
                ship.Label52x70Url = tokenResponse.Print52x70Url;
                _uow.ShippingRequests.Update(ship);
            }
            await _uow.SaveChangesAsync(cancellationToken);
        }
        return tokenResponse;
    }

    public async Task<GhnOrderInfoResponse> GetGhnOrderInfoAsync(ClaimsPrincipal userPrincipal, string orderCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(orderCode))
        {
            throw new InvalidOperationException("OrderCode là bắt buộc.");
        }

        var userId = GetUserId(userPrincipal);
        var ship = await _uow.ShippingRequests.Query()
            .FirstOrDefaultAsync(s => s.TrackingCode == orderCode, cancellationToken);
        if (ship == null)
        {
            throw new InvalidOperationException("Không tìm thấy vận chuyển với GHN code đã cung cấp.");
        }

        var order = await _uow.Orders.GetByIdAsync(ship.OrderId ?? 0);
        if (order == null || order.SellerId != userId)
        {
            throw new InvalidOperationException("Không được phép truy cập vận chuyển này.");
        }

        return await _ghnShippingService.GetOrderInfoAsync(orderCode, cancellationToken);
    }

    public async Task<BasicResponse> HandleGhnWebhookAsync(GhnWebhookPayload payload, string? webhookToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_ghnSettings.WebhookToken))
        {
            if (!string.Equals(_ghnSettings.WebhookToken, webhookToken, StringComparison.Ordinal))
            {
                return new BasicResponse(false, "Webhook token không hợp lệ.");
            }
        }

        if (string.IsNullOrWhiteSpace(payload.OrderCode))
        {
            return new BasicResponse(false, "OrderCode là bắt buộc.");
        }

        var ship = await _uow.ShippingRequests.Query()
            .FirstOrDefaultAsync(s => s.TrackingCode == payload.OrderCode, cancellationToken);
        if (ship == null)
        {
            return new BasicResponse(false, "Không tìm thấy đơn giao hàng.");
        }

        var mapped = MapGhnStatus(payload.Status);
        if (mapped == null)
        {
            return new BasicResponse(true, "Đã nhận webhook, không có thay đổi trạng thái.");
        }

        var current = ship.Status ?? ShippingStatuses.Requested;
        if (string.Equals(current, mapped, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(true, "Đơn giao hàng đã ở trạng thái yêu cầu.");
        }

        if (!IsShippingTransitionAllowed(current, mapped) &&
            !(string.Equals(current, ShippingStatuses.Requested, StringComparison.OrdinalIgnoreCase) &&
              string.Equals(mapped, ShippingStatuses.Delivered, StringComparison.OrdinalIgnoreCase)))
        {
            return new BasicResponse(false, $"Chuyển trạng thái giao hàng không hợp lệ: {current} -> {mapped}.");
        }

        ship.Status = mapped;
        if (string.IsNullOrWhiteSpace(ship.TrackingCode) && !string.IsNullOrWhiteSpace(payload.OrderCode))
        {
            ship.TrackingCode = payload.OrderCode;
        }

        await UpdateOrderStatusForShippingAsync(ship, mapped, cancellationToken);
        _uow.ShippingRequests.Update(ship);
        await _uow.SaveChangesAsync(cancellationToken);
        if (ship.OrderId.HasValue)
        {
            var order = await _uow.Orders.GetByIdAsync(ship.OrderId.Value);
            if (order != null)
            {
                await NotifyShippingStatusAsync(order, mapped, cancellationToken);
            }
        }
        return new BasicResponse(true, "Đã cập nhật giao hàng.");
    }

    public async Task<ShippingResponse?> GetByOrderAsync(ClaimsPrincipal userPrincipal, long orderId, CancellationToken cancellationToken = default)
    {
        var userId = GetUserId(userPrincipal);
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null) return null;
        if (order.BuyerId != userId && order.SellerId != userId) return null;

        var ship = await _uow.ShippingRequests.Query()
            .FirstOrDefaultAsync(s => s.OrderId == orderId, cancellationToken);
        if (ship == null) return null;

        return MapShippingResponse(ship);
    }

    public async Task<BasicResponse> UpdateStatusAsync(ClaimsPrincipal userPrincipal, long shipId, UpdateShippingStatusRequest request, CancellationToken cancellationToken = default)
    {
        ValidationGuard.ThrowIfInvalid(RequestValidator.ValidateUpdateShippingStatus(request));
        var userId = GetUserId(userPrincipal);
        var ship = await _uow.ShippingRequests.GetByIdAsync(shipId);
        if (ship == null) return new BasicResponse(false, "Không tìm thấy đơn giao hàng.");

        var order = await _uow.Orders.GetByIdAsync(ship.OrderId ?? 0);
        if (order == null || order.SellerId != userId) return new BasicResponse(false, "Không có quyền thực hiện.");

        if (!ShippingStatuses.All.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            return new BasicResponse(false, $"Trạng thái giao hàng không hợp lệ. Cho phép: {string.Join(", ", ShippingStatuses.All)}.");
        }

        if (string.Equals(ship.Status, request.Status, StringComparison.OrdinalIgnoreCase))
        {
            return new BasicResponse(true, "Đơn giao hàng đã ở trạng thái yêu cầu.");
        }

        var current = ship.Status ?? ShippingStatuses.Requested;
        if (!IsShippingTransitionAllowed(current, request.Status))
        {
            return new BasicResponse(false, $"Chuyển trạng thái giao hàng không hợp lệ: {current} -> {request.Status}.");
        }

        ship.Status = request.Status;
        if (!string.IsNullOrWhiteSpace(request.TrackingCode))
        {
            ship.TrackingCode = request.TrackingCode;
        }
        await UpdateOrderStatusForShippingAsync(ship, request.Status, cancellationToken);
        _uow.ShippingRequests.Update(ship);
        await _uow.SaveChangesAsync(cancellationToken);
        if (order != null)
        {
            await NotifyShippingStatusAsync(order, request.Status, cancellationToken);
        }
        return new BasicResponse(true, "Đã cập nhật giao hàng.");
    }

    private static bool IsShippingTransitionAllowed(string current, string next)
    {
        if (string.Equals(current, ShippingStatuses.Requested, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ShippingStatuses.InTransit, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ShippingStatuses.Failed, StringComparison.OrdinalIgnoreCase);
        }
        if (string.Equals(current, ShippingStatuses.InTransit, StringComparison.OrdinalIgnoreCase))
        {
            return string.Equals(next, ShippingStatuses.Delivered, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(next, ShippingStatuses.Failed, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private static string? MapGhnStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status)) return null;
        var value = status.Trim().ToLowerInvariant();
        return value switch
        {
            "ready_to_pick" => ShippingStatuses.Requested,
            "picking" => ShippingStatuses.InTransit,
            "picked" => ShippingStatuses.InTransit,
            "storing" => ShippingStatuses.InTransit,
            "transporting" => ShippingStatuses.InTransit,
            "sorting" => ShippingStatuses.InTransit,
            "delivering" => ShippingStatuses.InTransit,
            "delivered" => ShippingStatuses.Delivered,
            "delivery_fail" => ShippingStatuses.Failed,
            "waiting_to_return" => ShippingStatuses.Failed,
            "return" => ShippingStatuses.Failed,
            "return_transporting" => ShippingStatuses.Failed,
            "return_sorting" => ShippingStatuses.Failed,
            "returning" => ShippingStatuses.Failed,
            "returned" => ShippingStatuses.Failed,
            "cancel" => ShippingStatuses.Failed,
            "exception" => ShippingStatuses.Failed,
            "damage" => ShippingStatuses.Failed,
            "lost" => ShippingStatuses.Failed,
            _ => ShippingStatuses.InTransit
        };
    }

    private async Task UpdateOrderStatusForShippingAsync(ShippingRequest ship, string status, CancellationToken cancellationToken)
    {
        if (ship.OrderId == null || ship.OrderId <= 0) return;
        var order = await _uow.Orders.GetByIdAsync(ship.OrderId.Value);
        if (order == null) return;

        if (string.Equals(order.Status, OrderStatuses.Completed, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(status, ShippingStatuses.Requested, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(order.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
            {
                order.Status = OrderStatuses.Delivering;
                _uow.Orders.Update(order);
            }
            return;
        }

        if (string.Equals(status, ShippingStatuses.InTransit, StringComparison.OrdinalIgnoreCase))
        {
            if (string.Equals(order.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
            {
                order.Status = OrderStatuses.Delivering;
                _uow.Orders.Update(order);
            }
            return;
        }

        if (!string.Equals(status, ShippingStatuses.Delivered, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.Equals(order.Status, OrderStatuses.Confirmed, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(order.Status, OrderStatuses.Delivering, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // For non-COD, require remaining payment before marking delivered
        if (!string.Equals(order.PaymentMethod, PaymentMethods.COD, StringComparison.OrdinalIgnoreCase))
        {
            var hasRemainingPaid = await _uow.Payments.Query()
                .AnyAsync(p => p.OrderId == order.OrderId &&
                               p.PaymentStage == PaymentStages.Remaining &&
                               p.Status == PaymentStatuses.Paid, cancellationToken);
            if (!hasRemainingPaid)
            {
                return;
            }
        }

        order.Status = OrderStatuses.Delivered;
        _uow.Orders.Update(order);
    }

    private static ShippingResponse MapShippingResponse(ShippingRequest ship)
    {
        return new ShippingResponse(
            ship.ShipId,
            ship.OrderId ?? 0,
            ship.Provider,
            ship.TrackingCode,
            ship.PickupAddress,
            ship.DeliveryAddress,
            ship.Status,
            ship.CreatedAt,
            ship.LabelA5Url,
            ship.Label80x80Url,
            ship.Label52x70Url);
    }

    private async Task NotifyShippingStatusAsync(Order order, string status, CancellationToken cancellationToken)
    {
        var title = status switch
        {
            ShippingStatuses.Requested => "Đơn hàng đang được giao",
            ShippingStatuses.InTransit => "Đơn hàng đang vận chuyển",
            ShippingStatuses.Delivered => "Đơn hàng đã giao",
            ShippingStatuses.Failed => "Giao hàng thất bại",
            _ => "Cập nhật vận chuyển"
        };

        var message = $"Đơn hàng #{order.OrderId} - trạng thái vận chuyển: {status}.";
        if (order.BuyerId.HasValue)
        {
            await NotifyAsync(order.BuyerId.Value, "SHIPPING", title, message, $"/orders/{order.OrderId}", cancellationToken);
        }
        if (order.SellerId.HasValue)
        {
            await NotifyAsync(order.SellerId.Value, "SHIPPING", title, message, $"/orders/{order.OrderId}", cancellationToken);
        }
    }

    private async Task NotifyAsync(long userId, string type, string title, string message, string? link, CancellationToken cancellationToken)
    {
        try
        {
            await _notificationService.CreateAsync(new CreateNotificationRequest(
                userId,
                title,
                message,
                type,
                link), cancellationToken);
        }
        catch
        {
            // ignore notification failures
        }
    }
}












