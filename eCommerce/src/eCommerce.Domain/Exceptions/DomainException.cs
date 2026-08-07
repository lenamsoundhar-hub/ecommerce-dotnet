namespace eCommerce.Domain.Exceptions;

/// <summary>
/// Raised when an operation would leave an aggregate in an invalid state. The
/// Api layer maps this to a 400-family response.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }

    public DomainException(string message, Exception innerException)
        : base(message, innerException) { }
}
