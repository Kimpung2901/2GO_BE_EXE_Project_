namespace _2GO_EXE_Project.BAL.Validation;

public static class ValidationGuard
{
    public static void ThrowIfInvalid(ValidationResult result)
    {
        if (result == null || result.GetType().GetProperty("IsValid") == null || result.IsValid) return;
        throw CustomValidationException.FromResult(result);
    }
}
