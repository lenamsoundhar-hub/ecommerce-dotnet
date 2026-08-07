namespace eCommerce.Application.Common.Exceptions;

/// <summary>Input failed one or more validation rules, keyed by property name.</summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation failures occurred.") => Errors = errors;

    public IDictionary<string, string[]> Errors { get; }
}

/// <summary>The requested aggregate does not exist.</summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }

    public NotFoundException(string name, object key)
        : base($"{name} with key '{key}' was not found.") { }
}

/// <summary>The operation clashes with the current state, e.g. a duplicate SKU.</summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message) { }
}
