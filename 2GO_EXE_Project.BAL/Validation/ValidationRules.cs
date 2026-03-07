using System.Text.RegularExpressions;

namespace _2GO_EXE_Project.BAL.Validation;

public static class ValidationRules
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRegex = new(@"^\d{10}$", RegexOptions.Compiled);

    public static bool IsValidEmail(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return EmailRegex.IsMatch(value.Trim());
    }

    public static bool IsValidPhone(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return PhoneRegex.IsMatch(value.Trim());
    }
}
