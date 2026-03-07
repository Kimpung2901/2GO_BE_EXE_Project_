using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;
using _2GO_EXE_Project.BAL.DTOs.Carts;
using _2GO_EXE_Project.BAL.DTOs.Chat;
using _2GO_EXE_Project.BAL.DTOs.Listings;
using _2GO_EXE_Project.BAL.DTOs.Orders;
using _2GO_EXE_Project.BAL.DTOs.Payments;
using _2GO_EXE_Project.BAL.DTOs.Reports;
using _2GO_EXE_Project.BAL.DTOs.Ratings;
using _2GO_EXE_Project.BAL.DTOs.Shipping;
using _2GO_EXE_Project.BAL.DTOs.Comments;

namespace _2GO_EXE_Project.BAL.Validation;

public static partial class RequestValidator
{
    private const int ReasonMaxLength = 500;
    private const int EvidenceUrlMaxLength = 500;
    private const int EvidenceMaxCount = 5;
    private const int MessageMaxLength = 1000;
    private const int AddressMaxLength = 255;
    private const int ProviderMaxLength = 50;
    private const int TrackingCodeMaxLength = 100;
    private const int StatusMaxLength = 50;

    public static ValidationResult ValidateCreateOrder(CreateOrderRequest request)
    {
        var result = new ValidationResult();
        if (request.ListingId <= 0)
        {
            result.Add("listingId", "ListingId phải lớn hơn 0.");
        }
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            result.Add("paymentMethod", "Phương thức thanh toán là bắt buộc.");
        }
        if (string.IsNullOrWhiteSpace(request.DeliveryAddress))
        {
            result.Add("deliveryAddress", "Địa chỉ giao hàng là bắt buộc.");
        }
        else if (request.DeliveryAddress.Trim().Length > AddressMaxLength)
        {
            result.Add("deliveryAddress", "Địa chỉ giao hàng không được vượt quá 255 ký tự.");
        }
        if (string.IsNullOrWhiteSpace(request.DeliveryPhone))
        {
            result.Add("deliveryPhone", "Số điện thoại nhận hàng là bắt buộc.");
        }
        else if (!ValidationRules.IsValidPhone(request.DeliveryPhone))
        {
            result.Add("deliveryPhone", "Số điện thoại nhận hàng phải gồm đúng 10 chữ số.");
        }
        return result;
    }

    public static ValidationResult ValidateCreatePayment(CreatePaymentRequest request)
    {
        var result = new ValidationResult();
        if (request.OrderId <= 0)
        {
            result.Add("orderId", "OrderId phải lớn hơn 0.");
        }
        if (string.IsNullOrWhiteSpace(request.Method))
        {
            result.Add("method", "Phương thức thanh toán là bắt buộc.");
        }
        if (!string.IsNullOrWhiteSpace(request.PaymentStage) &&
            !PaymentStages.All.Contains(request.PaymentStage, StringComparer.OrdinalIgnoreCase))
        {
            result.Add("paymentStage", $"Giai đoạn thanh toán không hợp lệ. Cho phép: {string.Join(", ", PaymentStages.All)}.");
        }
        return result;
    }

    public static ValidationResult ValidateCreateSubscriptionPayment(CreateSubscriptionPaymentRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Method))
        {
            result.Add("method", "Phương thức thanh toán là bắt buộc.");
        }
        if (string.IsNullOrWhiteSpace(request.PlanCode))
        {
            result.Add("planCode", "Mã gói là bắt buộc.");
        }
        return result;
    }

    public static ValidationResult ValidateVerifyPayment(VerifyPaymentRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            result.Add("status", "Trạng thái là bắt buộc.");
        }
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Trim().Length > StatusMaxLength)
        {
            result.Add("status", "Trạng thái không được vượt quá 50 ký tự.");
        }
        return result;
    }

    public static ValidationResult ValidateCreateShipping(CreateShippingRequest request)
    {
        var result = new ValidationResult();
        if (request.OrderId <= 0)
        {
            result.Add("orderId", "OrderId phải lớn hơn 0.");
        }
        if (string.IsNullOrWhiteSpace(request.Provider))
        {
            result.Add("provider", "Nhà vận chuyển là bắt buộc.");
        }
        else if (request.Provider.Trim().Length > ProviderMaxLength)
        {
            result.Add("provider", "Nhà vận chuyển không được vượt quá 50 ký tự.");
        }
        if (string.IsNullOrWhiteSpace(request.PickupAddress))
        {
            result.Add("pickupAddress", "Địa chỉ lấy hàng là bắt buộc.");
        }
        else if (request.PickupAddress.Trim().Length > AddressMaxLength)
        {
            result.Add("pickupAddress", "Địa chỉ lấy hàng không được vượt quá 255 ký tự.");
        }
        if (!string.IsNullOrWhiteSpace(request.DeliveryAddress) && request.DeliveryAddress.Trim().Length > AddressMaxLength)
        {
            result.Add("deliveryAddress", "Địa chỉ giao hàng không được vượt quá 255 ký tự.");
        }
        return result;
    }

    public static ValidationResult ValidateCreateGhnShipping(CreateGhnShippingRequest request)
    {
        var result = new ValidationResult();
        if (request.OrderId <= 0) result.Add("orderId", "OrderId phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(request.ToName)) result.Add("toName", "Tên người nhận là bắt buộc.");
        if (string.IsNullOrWhiteSpace(request.ToPhone))
        {
            result.Add("toPhone", "Số điện thoại người nhận là bắt buộc.");
        }
        else if (!ValidationRules.IsValidPhone(request.ToPhone))
        {
            result.Add("toPhone", "Số điện thoại người nhận phải gồm đúng 10 chữ số.");
        }
        if (string.IsNullOrWhiteSpace(request.ToAddress)) result.Add("toAddress", "Địa chỉ người nhận là bắt buộc.");
        if (string.IsNullOrWhiteSpace(request.ToWardCode)) result.Add("toWardCode", "Mã phường/xã là bắt buộc.");
        if (request.ToDistrictId <= 0) result.Add("toDistrictId", "ToDistrictId phải lớn hơn 0.");
        if (request.Weight <= 0) result.Add("weight", "Khối lượng phải lớn hơn 0.");
        if (request.Length <= 0) result.Add("length", "Chiều dài phải lớn hơn 0.");
        if (request.Width <= 0) result.Add("width", "Chiều rộng phải lớn hơn 0.");
        if (request.Height <= 0) result.Add("height", "Chiều cao phải lớn hơn 0.");
        if (request.ServiceTypeId <= 0) result.Add("serviceTypeId", "ServiceTypeId phải lớn hơn 0.");
        if (request.PaymentTypeId <= 0) result.Add("paymentTypeId", "PaymentTypeId phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(request.RequiredNote)) result.Add("requiredNote", "RequiredNote là bắt buộc.");
        if (request.Items == null || request.Items.Count == 0)
        {
            result.Add("items", "Cần ít nhất một sản phẩm.");
            return result;
        }
        for (var i = 0; i < request.Items.Count; i++)
        {
            var item = request.Items[i];
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                result.Add($"items[{i}].name", "Tên sản phẩm là bắt buộc.");
            }
            if (item.Quantity <= 0) result.Add($"items[{i}].quantity", "Số lượng phải lớn hơn 0.");
            if (item.Weight <= 0) result.Add($"items[{i}].weight", "Khối lượng phải lớn hơn 0.");
        }
        return result;
    }

    public static ValidationResult ValidateGhnFee(GhnFeeRequest request)
    {
        var result = new ValidationResult();
        if (request.ToDistrictId <= 0) result.Add("toDistrictId", "ToDistrictId phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(request.ToWardCode)) result.Add("toWardCode", "Mã phường/xã là bắt buộc.");
        if (request.Weight <= 0) result.Add("weight", "Khối lượng phải lớn hơn 0.");
        if (request.FromDistrictId <= 0) result.Add("fromDistrictId", "FromDistrictId phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(request.FromWardCode)) result.Add("fromWardCode", "FromWardCode là bắt buộc.");
        return result;
    }

    public static ValidationResult ValidateGhnCancel(GhnCancelRequest request)
    {
        var result = new ValidationResult();
        if (request.OrderCodes == null || request.OrderCodes.Count == 0)
        {
            result.Add("orderCodes", "Cần ít nhất một mã đơn hàng.");
        }
        return result;
    }

    public static ValidationResult ValidateGhnPrintToken(GhnPrintTokenRequest request)
    {
        var result = new ValidationResult();
        if (request.OrderCodes == null || request.OrderCodes.Count == 0)
        {
            result.Add("orderCodes", "Cần ít nhất một mã đơn hàng.");
        }
        return result;
    }

    public static ValidationResult ValidateUpdateShippingStatus(UpdateShippingStatusRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            result.Add("status", "Trạng thái là bắt buộc.");
        }
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Trim().Length > StatusMaxLength)
        {
            result.Add("status", "Trạng thái không được vượt quá 50 ký tự.");
        }
        if (!string.IsNullOrWhiteSpace(request.TrackingCode) && request.TrackingCode.Trim().Length > TrackingCodeMaxLength)
        {
            result.Add("trackingCode", "TrackingCode không được vượt quá 100 ký tự.");
        }
        return result;
    }

    public static ValidationResult ValidateAddCartItem(AddCartItemRequest request)
    {
        var result = new ValidationResult();
        if (request.ListingId <= 0) result.Add("listingId", "ListingId phải lớn hơn 0.");
        if (request.Quantity <= 0) result.Add("quantity", "Số lượng phải lớn hơn 0.");
        return result;
    }

    public static ValidationResult ValidateUpdateCartItem(UpdateCartItemRequest request)
    {
        var result = new ValidationResult();
        if (request.Quantity <= 0) result.Add("quantity", "Số lượng phải lớn hơn 0.");
        return result;
    }

    public static ValidationResult ValidateCheckoutCart(CheckoutCartRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.PaymentMethod))
        {
            result.Add("paymentMethod", "Phương thức thanh toán là bắt buộc.");
        }
        return result;
    }

    public static ValidationResult ValidateCreateChat(CreateChatRequest request)
    {
        var result = new ValidationResult();
        if (request.OtherUserId <= 0)
        {
            result.Add("otherUserId", "OtherUserId phải lớn hơn 0.");
        }
        return result;
    }

    public static ValidationResult ValidateSendMessage(SendMessageRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Content) && string.IsNullOrWhiteSpace(request.ImageUrl))
        {
            result.Add("content", "Nội dung hoặc ảnh là bắt buộc.");
            result.Add("imageUrl", "Nội dung hoặc ảnh là bắt buộc.");
        }
        if (!string.IsNullOrWhiteSpace(request.Content) && request.Content.Trim().Length > MessageMaxLength)
        {
            result.Add("content", "Nội dung không được vượt quá 1000 ký tự.");
        }
        return result;
    }

    public static ValidationResult ValidateSaveListing(SaveListingRequest request)
    {
        var result = new ValidationResult();
        if (request.ListingId <= 0)
        {
            result.Add("listingId", "ListingId phải lớn hơn 0.");
        }
        return result;
    }

    public static ValidationResult ValidateCreateReport(CreateReportRequest request)
    {
        var result = new ValidationResult();
        if (request.OrderId <= 0) result.Add("orderId", "OrderId phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            result.Add("reason", "Lý do là bắt buộc.");
        }
        else if (request.Reason.Trim().Length > ReasonMaxLength)
        {
            result.Add("reason", "Lý do không được vượt quá 500 ký tự.");
        }
        if (request.EvidenceUrls != null)
        {
            if (request.EvidenceUrls.Count > EvidenceMaxCount)
            {
                result.Add("evidenceUrls", $"EvidenceUrls không được vượt quá {EvidenceMaxCount} mục.");
            }
            for (var i = 0; i < request.EvidenceUrls.Count; i++)
            {
                var url = request.EvidenceUrls[i];
                if (string.IsNullOrWhiteSpace(url))
                {
                    result.Add($"evidenceUrls[{i}]", "Evidence url là bắt buộc.");
                    continue;
                }
                if (url.Trim().Length > EvidenceUrlMaxLength)
                {
                    result.Add($"evidenceUrls[{i}]", "Evidence url không được vượt quá 500 ký tự.");
                }
            }
        }
        return result;
    }

    public static ValidationResult ValidateReplyReport(ReplyReportRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            result.Add("message", "Nội dung là bắt buộc.");
        }
        else if (request.Message.Trim().Length > MessageMaxLength)
        {
            result.Add("message", "Nội dung không được vượt quá 1000 ký tự.");
        }
        return result;
    }

    public static ValidationResult ValidateResolveReport(ResolveReportRequest request)
    {
        var result = new ValidationResult();
        if (!string.IsNullOrWhiteSpace(request.Status) && request.Status.Trim().Length > StatusMaxLength)
        {
            result.Add("status", "Trạng thái không được vượt quá 50 ký tự.");
        }
        if (!string.IsNullOrWhiteSpace(request.Decision) && request.Decision.Trim().Length > StatusMaxLength)
        {
            result.Add("decision", "Decision không được vượt quá 50 ký tự.");
        }
        return result;
    }

    public static ValidationResult ValidateCreateRating(CreateUserRatingRequest request)
    {
        var result = new ValidationResult();
        if (request.OrderId <= 0)
        {
            result.Add("orderId", "OrderId phải lớn hơn 0.");
        }
        if (request.Score < 1 || request.Score > 5)
        {
            result.Add("score", "Điểm phải từ 1 đến 5.");
        }
        return result;
    }

    public static ValidationResult ValidateUpdateListingStatus(UpdateListingStatusRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            result.Add("status", "Trạng thái là bắt buộc.");
        }
        else if (!ListingStatuses.All.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            result.Add("status", $"Trạng thái bài đăng không hợp lệ. Cho phép: {string.Join(", ", ListingStatuses.All)}.");
        }
        return result;
    }

    public static ValidationResult ValidateUpdateOrderStatus(UpdateOrderStatusRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Status))
        {
            result.Add("status", "Trạng thái là bắt buộc.");
        }
        else if (!OrderStatuses.All.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            result.Add("status", $"Trạng thái đơn hàng không hợp lệ. Cho phép: {string.Join(", ", OrderStatuses.All)}.");
        }
        if (!string.IsNullOrWhiteSpace(request.Reason) && request.Reason.Trim().Length > ReasonMaxLength)
        {
            result.Add("reason", $"Lý do không được vượt quá {ReasonMaxLength} ký tự.");
        }
        return result;
    }

    public static ValidationResult ValidateRejectListing(RejectListingRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            result.Add("reason", "Lý do là bắt buộc.");
        }
        return result;
    }

    public static ValidationResult ValidateFlagListing(FlagListingRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            result.Add("reason", "Lý do là bắt buộc.");
        }
        return result;
    }

    public static ValidationResult ValidateCreateComment(CreateCommentRequest request)
    {
        var result = new ValidationResult();

        if (request.ListingId <= 0)
            result.Add("listingId", "ListingId phải lớn hơn 0.");

        if (string.IsNullOrWhiteSpace(request.Content))
            result.Add("content", "Nội dung là bắt buộc.");
        else if (request.Content.Length > 2000)
            result.Add("content", "Nội dung không được vượt quá 2000 ký tự.");

        if (request.ParentId.HasValue && request.ParentId.Value <= 0)
            result.Add("parentId", "ParentId phải lớn hơn 0 khi có cung cấp.");

        return result;
    }

    public static ValidationResult ValidateUpdateComment(UpdateCommentRequest request)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(request.Content))
            result.Add("content", "Nội dung là bắt buộc.");
        else if (request.Content.Length > 2000)
            result.Add("content", "Nội dung không được vượt quá 2000 ký tự.");

        return result;
    }
}









