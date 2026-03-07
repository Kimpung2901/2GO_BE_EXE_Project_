using System;
using System.Collections.Generic;

namespace _2GO_EXE_Project.BAL.Validation;

public sealed class CustomValidationException : Exception
{
    public IReadOnlyList<ValidationError> Errors { get; }
    public string? Code { get; }

    public CustomValidationException(IReadOnlyList<ValidationError> errors, string? code = null)
        : base(errors.Count > 0 ? errors[0].Message : "Dữ liệu không hợp lệ.")
    {
        Errors = errors;
        Code = code;
    }

    public static CustomValidationException FromResult(ValidationResult result)
    {
        return new CustomValidationException(result.Errors);
    }
}


