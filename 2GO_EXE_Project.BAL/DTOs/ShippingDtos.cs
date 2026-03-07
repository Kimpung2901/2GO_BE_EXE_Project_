namespace _2GO_EXE_Project.BAL.DTOs.Shipping;

public record CreateShippingRequest(long OrderId, string Provider, string PickupAddress, string? DeliveryAddress);

public record GhnItemRequest(
    string Name,
    int Quantity,
    long Price,
    int Weight,
    string? Code = null,
    int? Length = null,
    int? Width = null,
    int? Height = null);

public record CreateGhnShippingRequest(
    long OrderId,
    string ToName,
    string ToPhone,
    string ToAddress,
    string ToWardCode,
    int ToDistrictId,
    int Weight,
    int Length,
    int Width,
    int Height,
    int ServiceTypeId,
    int PaymentTypeId,
    string RequiredNote,
    string? Note,
    IReadOnlyList<GhnItemRequest> Items,
    string? FromName = null,
    string? FromPhone = null,
    string? FromAddress = null,
    string? FromWardCode = null,
    int? FromDistrictId = null);

public record GhnProvinceResponse(int ProvinceId, string ProvinceName);

public record GhnDistrictResponse(int DistrictId, string DistrictName, int ProvinceId);

public record GhnWardResponse(string WardCode, string WardName, int DistrictId);

public record GhnFeeRequest(
    int FromDistrictId,
    string FromWardCode,
    int ToDistrictId,
    string ToWardCode,
    int Weight,
    int Length,
    int Width,
    int Height,
    int ServiceTypeId,
    long? InsuranceValue = null,
    int? ServiceId = null);

public record GhnFeeResponse(
    long Total,
    long ServiceFee,
    long InsuranceFee,
    long PickStationFee,
    long CouponValue,
    long R2SFee);

public record GhnCancelRequest(IReadOnlyList<string> OrderCodes);

public record GhnCancelResult(string OrderCode, bool Result, string? Message);

public record GhnCancelResponse(IReadOnlyList<GhnCancelResult> Results);

public record GhnPrintTokenRequest(IReadOnlyList<string> OrderCodes);

public record GhnPrintTokenResponse(string Token, string PrintA5Url, string Print80x80Url, string Print52x70Url);

public record GhnOrderInfoResponse(
    string OrderCode,
    string? Status,
    string? StatusName,
    string? UpdatedDate,
    string? Raw);

public record GhnWebhookPayload(
    string? OrderCode,
    string? Status,
    string? StatusName,
    int? StatusId,
    string? UpdatedAt,
    string? Raw);

public record UpdateShippingStatusRequest(string Status, string? TrackingCode);

public record ShippingResponse(
    long ShipId,
    long OrderId,
    string? Provider,
    string? TrackingCode,
    string? PickupAddress,
    string? DeliveryAddress,
    string? Status,
    DateTime? CreatedAt,
    string? LabelA5Url = null,
    string? Label80x80Url = null,
    string? Label52x70Url = null);
