namespace _2GO_EXE_Project.BAL.Constants;

public static class UserRoles
{
    public const string User = "User";
    public const string Manager = "Manager";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> All = new[]
    {
        User,
        Manager,
        Admin
    };

    public static string Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role)) return User;
        if (string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase)) return Admin;
        if (string.Equals(role, "Moderator", StringComparison.OrdinalIgnoreCase)) return Manager;
        if (string.Equals(role, Manager, StringComparison.OrdinalIgnoreCase)) return Manager;
        return User;
    }
}
