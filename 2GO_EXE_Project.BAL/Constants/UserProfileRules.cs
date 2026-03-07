namespace _2GO_EXE_Project.BAL.Constants;

public static class UserProfileRules
{
    public const int GenderMaxLength = 50;

    public static readonly IReadOnlyList<string> AllowedGenders = new[]
    {
        "Male",
        "Female",
        "Other"
    };

    public static string? Validate(string? gender, DateOnly? birthday)
    {
        if (birthday.HasValue)
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            if (birthday.Value > today) return "Ngày sinh không thể ở tương lai.";
            if (birthday.Value < today.AddYears(-120)) return "Ngày sinh quá xa trong quá khứ.";
        }

        if (!string.IsNullOrWhiteSpace(gender))
        {
            var trimmed = gender.Trim();
            if (trimmed.Length > GenderMaxLength) return "Giới tính không được vượt quá 50 ký tự.";
            if (!AllowedGenders.Any(g => string.Equals(g, trimmed, StringComparison.OrdinalIgnoreCase)))
            {
                return $"Giới tính không hợp lệ. Cho phép: {string.Join(", ", AllowedGenders)}.";
            }
        }

        return null;
    }
}

