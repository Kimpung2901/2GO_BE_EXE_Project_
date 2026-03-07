using _2GO_EXE_Project.BAL.DTOs.Subscriptions;

namespace _2GO_EXE_Project.BAL.Validation;

public static class SubscriptionPlanValidator
{
    public static ValidationResult ValidateCreate(CreateSubscriptionPlanRequest request)
    {
        return ValidatePlan(request.Code, request.Name, request.Price, request.DurationDays, request.MonthlyListingLimit, request.SortOrder, validateCode: true);
    }

    public static ValidationResult ValidateUpdate(UpdateSubscriptionPlanRequest request)
    {
        return ValidatePlan("DUMMY", request.Name, request.Price, request.DurationDays, request.MonthlyListingLimit, request.SortOrder, validateCode: false);
    }

    public static ValidationResult ValidatePrice(UpdateSubscriptionPlanPriceRequest request)
    {
        var result = new ValidationResult();
        if (request.Price < 0)
        {
            result.Add("price", "Giá phải lớn hon hoặc bằng 0.");
        }
        return result;
    }

    private static ValidationResult ValidatePlan(string code, string name, decimal price, int durationDays, int? monthlyLimit, int sortOrder, bool validateCode)
    {
        var result = new ValidationResult();
        if (validateCode)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                result.Add("code", "Mã là bắt buộc.");
            }
            else if (code.Trim().Length > 50)
            {
                result.Add("code", "Mã không được vượt quá 50 ký tự.");
            }
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            result.Add("name", "Tên là bắt buộc.");
        }
        else if (name.Trim().Length > 255)
        {
            result.Add("name", "Tên không được vượt quá 255 ký tự.");
        }

        if (price < 0)
        {
            result.Add("price", "Giá phải lớn hon hoặc bằng 0.");
        }

        if (durationDays <= 0)
        {
            result.Add("durationDays", "DurationDays phải lớn hơn 0.");
        }

        if (monthlyLimit.HasValue && monthlyLimit.Value <= 0)
        {
            result.Add("monthlyListingLimit", "MonthlyListingLimit phải lớn hơn 0 khi có cung cấp.");
        }

        if (sortOrder < 0)
        {
            result.Add("sortOrder", "SortOrder phải lớn hon hoặc bằng 0.");
        }

        return result;
    }
}







