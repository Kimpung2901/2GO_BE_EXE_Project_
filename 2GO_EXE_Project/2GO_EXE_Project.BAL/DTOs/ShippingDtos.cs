namespace _2GO_EXE_Project.BAL.DTOs.Shipping;

public record CreateShippingRequest(long OrderId, string Provider, string PickupAddress, string DeliveryAddress);

public record UpdateShippingStatusRequest(string Status, string? TrackingCode);

public record ShippingResponse(
    long ShipId,
    long OrderId,
    string? Provider,
    string? TrackingCode,
    string? PickupAddress,
    string? DeliveryAddress,
    string? Status,
    DateTime? CreatedAt);
