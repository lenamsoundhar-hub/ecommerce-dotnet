namespace eCommerce.Domain.Common;

/// <summary>
/// Marks an entity whose audit timestamps are maintained by the persistence
/// layer rather than by domain code.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedOnUtc { get; set; }

    DateTimeOffset? ModifiedOnUtc { get; set; }
}
