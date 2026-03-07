using System.Collections.Generic;
using System.Linq;

namespace _2GO_EXE_Project.BAL.Validation;

public sealed class ValidationResult
{
    private readonly List<ValidationError> _errors = new();

    public IReadOnlyList<ValidationError> Errors => _errors;
    public bool IsValid => _errors.Count == 0;

    public List<string> ErrorMessages => _errors.Select(e => e.Message).ToList();

    public void Add(string field, string message)
    {
        _errors.Add(new ValidationError(field, message));
    }

    public void AddIf(bool condition, string field, string message)
    {
        if (condition) Add(field, message);
    }

    public void Merge(ValidationResult other)
    {
        if (other == null || other.IsValid) return;
        _errors.AddRange(other.Errors);
    }

    public string FirstMessageOrDefault(string fallback = "Dữ liệu không hợp lệ.")
    {
        return _errors.FirstOrDefault()?.Message ?? fallback;
    }
}


