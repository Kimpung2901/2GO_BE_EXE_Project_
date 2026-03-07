using _2GO_EXE_Project.BAL.Constants;
using _2GO_EXE_Project.BAL.DTOs.Auth;

namespace _2GO_EXE_Project.BAL.Validation;

public static class UserValidator
{
    private const int EmailMaxLength = 255;
    private const int PhoneMaxLength = 20;
    private const int FullNameMaxLength = 255;
    private const int AvatarUrlMaxLength = 500;
    private const int AddressMaxLength = 255;
    private const int BankAccountNumberMaxLength = 50;
    private const int BankBinMaxLength = 50;
    private const int BankAccountNameMaxLength = 255;

    public static ValidationResult ValidateRegister(RegisterRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            result.Add("email", "Email hoặc số điện thoại là bắt buộc.");
            result.Add("phone", "Email hoặc số điện thoại là bắt buộc.");
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            result.AddIf(email.Length > EmailMaxLength, "email", "Email không được vượt quá 255 ký tự.");
            result.AddIf(!ValidationRules.IsValidEmail(email), "email", "Email không hợp lệ.");
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();
            result.AddIf(phone.Length > PhoneMaxLength, "phone", "Số điện thoại không được vượt quá 20 ký tự.");
            result.AddIf(!ValidationRules.IsValidPhone(phone), "phone", "Số điện thoại phải gồm đúng 10 chữ số.");
        }

        if (!IsValidPassword(request.Password))
        {
            result.Add("password", "Mật khẩu phải có ít nhất 8 ký tự và bao gồm ít nhất 1 chữ cái và 1 chữ số.");
        }

        if (!string.IsNullOrWhiteSpace(request.FullName))
        {
            result.AddIf(request.FullName.Trim().Length > FullNameMaxLength, "fullName", "Họ tên không được vượt quá 255 ký tự.");
        }

        return result;
    }

    public static ValidationResult ValidateLogin(LoginRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Identifier))
        {
            result.Add("identifier", "Tài khoản đăng nhập là bắt buộc.");
            return result;
        }

        var identifier = request.Identifier.Trim();
        var isEmail = ValidationRules.IsValidEmail(identifier);
        var isPhone = ValidationRules.IsValidPhone(identifier);
        result.AddIf(!isEmail && !isPhone, "identifier", "Tài khoản đăng nhập phải là email hoặc số điện thoại hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            result.Add("password", "Mật khẩu là bắt buộc.");
        }

        return result;
    }

    public static ValidationResult ValidateVerifyEmail(VerifyEmailRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            result.Add("email", "Email là bắt buộc.");
        }
        else
        {
            result.AddIf(!ValidationRules.IsValidEmail(request.Email), "email", "Email không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            result.Add("code", "Mã xác thực là bắt buộc.");
        }

        return result;
    }

    public static ValidationResult ValidateResendVerifyEmail(ResendVerifyEmailRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            result.Add("email", "Email là bắt buộc.");
        }
        else
        {
            result.AddIf(!ValidationRules.IsValidEmail(request.Email), "email", "Email không hợp lệ.");
        }
        return result;
    }

    public static ValidationResult ValidateForgotPassword(ForgotPasswordRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            result.Add("email", "Email là bắt buộc.");
        }
        else
        {
            result.AddIf(!ValidationRules.IsValidEmail(request.Email), "email", "Email không hợp lệ.");
        }
        return result;
    }

    public static ValidationResult ValidateResetPassword(ResetPasswordRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            result.Add("email", "Email là bắt buộc.");
        }
        else
        {
            result.AddIf(!ValidationRules.IsValidEmail(request.Email), "email", "Email không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(request.Code))
        {
            result.Add("code", "Mã xác thực là bắt buộc.");
        }

        if (!IsValidPassword(request.NewPassword))
        {
            result.Add("newPassword", "Mật khẩu phải có ít nhất 8 ký tự và bao gồm ít nhất 1 chữ cái và 1 chữ số.");
        }

        return result;
    }

    public static ValidationResult ValidateRefreshToken(RefreshTokenRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            result.Add("refreshToken", "Refresh token là bắt buộc.");
        }
        return result;
    }

    public static ValidationResult ValidateFirebaseLogin(FirebaseLoginRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            result.Add("idToken", "IdToken là bắt buộc.");
        }
        return result;
    }

    public static ValidationResult ValidateVerifyPhoneFirebase(VerifyPhoneFirebaseRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            result.Add("idToken", "IdToken là bắt buộc.");
        }
        return result;
    }

    public static ValidationResult ValidateChangePhoneFirebase(ChangePhoneFirebaseRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.IdToken))
        {
            result.Add("idToken", "IdToken là bắt buộc.");
        }
        return result;
    }

    public static ValidationResult ValidateAdminCreate(AdminCreateUserRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Email) && string.IsNullOrWhiteSpace(request.Phone))
        {
            result.Add("email", "Email hoặc số điện thoại là bắt buộc.");
            result.Add("phone", "Email hoặc số điện thoại là bắt buộc.");
        }
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            result.AddIf(email.Length > EmailMaxLength, "email", "Email không được vượt quá 255 ký tự.");
            result.AddIf(!ValidationRules.IsValidEmail(email), "email", "Email không hợp lệ.");
        }
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();
            result.AddIf(phone.Length > PhoneMaxLength, "phone", "Số điện thoại không được vượt quá 20 ký tự.");
            result.AddIf(!ValidationRules.IsValidPhone(phone), "phone", "Số điện thoại phải gồm đúng 10 chữ số.");
        }

        if (!UserRoles.All.Contains(request.Role ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            result.Add("role", "Vai trò không hợp lệ. Cho phép: User, Manager, Admin.");
        }

        if (!UserStatuses.All.Contains(request.Status ?? string.Empty, StringComparer.OrdinalIgnoreCase))
        {
            result.Add("status", "Trạng thái không hợp lệ. Cho phép: Active, Banned, Deleted.");
        }

        if (!string.IsNullOrEmpty(request.Password) && !IsValidPassword(request.Password))
        {
            result.Add("password", "Mật khẩu phải có ít nhất 8 ký tự và bao gồm ít nhất 1 chữ cái và 1 chữ số.");
        }

        result.Merge(ValidateProfileFields(request.FullName, request.Birthday, request.Gender, request.Address, request.Bio, request.AvatarUrl, null, null, null));
        return result;
    }

    public static ValidationResult ValidateAdminUpdate(UpdateUserRequest request)
    {
        var result = new ValidationResult();

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var email = request.Email.Trim();
            result.AddIf(email.Length > EmailMaxLength, "email", "Email không được vượt quá 255 ký tự.");
            result.AddIf(!ValidationRules.IsValidEmail(email), "email", "Email không hợp lệ.");
        }
        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();
            result.AddIf(phone.Length > PhoneMaxLength, "phone", "Số điện thoại không được vượt quá 20 ký tự.");
            result.AddIf(!ValidationRules.IsValidPhone(phone), "phone", "Số điện thoại phải gồm đúng 10 chữ số.");
        }
        if (!string.IsNullOrWhiteSpace(request.Status) && !UserStatuses.All.Contains(request.Status, StringComparer.OrdinalIgnoreCase))
        {
            result.Add("status", "Trạng thái không hợp lệ. Cho phép: Active, Banned, Deleted.");
        }

        result.Merge(ValidateProfileFields(request.FullName, request.Birthday, request.Gender, request.Address, request.Bio, request.AvatarUrl, null, null, null));
        return result;
    }

    public static ValidationResult ValidateUpdateProfile(UpdateProfileRequest request)
    {
        var result = ValidateProfileFields(request.FullName, request.Birthday, request.Gender, request.Address, request.Bio, request.AvatarUrl, request.BankAccountNumber, request.BankBin, request.BankAccountName);

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();
            result.AddIf(phone.Length > PhoneMaxLength, "phone", "Số điện thoại không được vượt quá 20 ký tự.");
            result.AddIf(!ValidationRules.IsValidPhone(phone), "phone", "Số điện thoại phải gồm đúng 10 chữ số.");
        }

        return result;
    }

    public static ValidationResult ValidateUpdateAvatar(UpdateAvatarRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.AvatarUrl))
        {
            result.Add("avatarUrl", "AvatarUrl là bắt buộc.");
            return result;
        }

        var trimmed = request.AvatarUrl.Trim();
        result.AddIf(trimmed.Length > AvatarUrlMaxLength, "avatarUrl", "AvatarUrl không được vượt quá 500 ký tự.");
        return result;
    }

    public static ValidationResult ValidateUpdateAddress(UpdateAddressRequest request)
    {
        var result = new ValidationResult();
        if (request.Address == null && request.CityId == null && request.DistrictId == null && request.WardId == null)
        {
            result.Add("address", "Cần ít nhất một thông tin địa chỉ.");
        }
        if (!string.IsNullOrWhiteSpace(request.Address))
        {
            result.AddIf(request.Address.Trim().Length > AddressMaxLength, "address", "Địa chỉ không được vượt quá 255 ký tự.");
        }
        return result;
    }

    public static ValidationResult ValidateChangePassword(ChangePasswordRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.CurrentPassword))
        {
            result.Add("currentPassword", "Current Mật khẩu là bắt buộc.");
        }
        if (!IsValidPassword(request.NewPassword))
        {
            result.Add("newPassword", "Mật khẩu phải có ít nhất 8 ký tự và bao gồm ít nhất 1 chữ cái và 1 chữ số.");
        }
        return result;
    }

    public static ValidationResult ValidateBanUser(BanUserRequest request)
    {
        var result = new ValidationResult();
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            result.Add("reason", "Lý do là bắt buộc.");
        }
        if (request.DurationDays.HasValue && request.DurationDays.Value <= 0)
        {
            result.Add("durationDays", "DurationDays phải lớn hơn 0.");
        }
        return result;
    }

    private static ValidationResult ValidateProfileFields(
        string? fullName,
        DateOnly? birthday,
        string? gender,
        string? address,
        string? bio,
        string? avatarUrl,
        string? bankAccountNumber,
        string? bankBin,
        string? bankAccountName)
    {
        var result = new ValidationResult();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            result.AddIf(fullName.Trim().Length > FullNameMaxLength, "fullName", "Họ tên không được vượt quá 255 ký tự.");
        }
        if (!string.IsNullOrWhiteSpace(address))
        {
            result.AddIf(address.Trim().Length > AddressMaxLength, "address", "Địa chỉ không được vượt quá 255 ký tự.");
        }
        if (!string.IsNullOrWhiteSpace(avatarUrl))
        {
            result.AddIf(avatarUrl.Trim().Length > AvatarUrlMaxLength, "avatarUrl", "AvatarUrl không được vượt quá 500 ký tự.");
        }
        if (!string.IsNullOrWhiteSpace(bankAccountNumber))
        {
            result.AddIf(bankAccountNumber.Trim().Length > BankAccountNumberMaxLength, "bankAccountNumber", "Số tài khoản không được vượt quá 50 ký tự.");
        }
        if (!string.IsNullOrWhiteSpace(bankBin))
        {
            var trimmedBin = bankBin.Trim();
            result.AddIf(trimmedBin.Length > BankBinMaxLength, "bankBin", "BankBin không được vượt quá 50 ký tự.");
            result.AddIf(!trimmedBin.All(char.IsDigit), "bankBin", "BankBin chỉ được chưa chữ số.");
        }
        if (!string.IsNullOrWhiteSpace(bankAccountName))
        {
            result.AddIf(bankAccountName.Trim().Length > BankAccountNameMaxLength, "bankAccountName", "Tên chủ tài khoản không được vượt quá 255 ký tự.");
        }

        var normalizedGender = NormalizeGender(gender);
        var profileRulesError = UserProfileRules.Validate(normalizedGender, birthday);
        if (profileRulesError != null)
        {
            result.Add("profile", profileRulesError);
        }

        return result;
    }

    private static bool IsValidPassword(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 8) return false;
        var hasLetter = value.Any(char.IsLetter);
        var hasDigit = value.Any(char.IsDigit);
        return hasLetter && hasDigit;
    }

    private static string? NormalizeGender(string? gender)
    {
        if (gender == null) return null;
        var trimmed = gender.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }
}








