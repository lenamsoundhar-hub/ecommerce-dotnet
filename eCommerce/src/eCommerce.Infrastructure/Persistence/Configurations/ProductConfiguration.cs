using eCommerce.Domain.Catalog;
using eCommerce.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace eCommerce.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .ValueGeneratedNever();

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(Product.NameMaxLength);

        builder.Property(p => p.Description)
            .IsRequired()
            .HasMaxLength(Product.DescriptionMaxLength);

        // Single-value object, so a converter is a better fit than an owned type.
        builder.Property(p => p.Sku)
            .IsRequired()
            .HasMaxLength(Sku.MaxLength)
            .HasConversion(sku => sku.Value, value => Sku.From(value));

        builder.HasIndex(p => p.Sku)
            .IsUnique();

        builder.OwnsOne(p => p.Price, price =>
        {
            price.Property(m => m.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2)
                .IsRequired();

            price.Property(m => m.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(Money.CurrencyCodeLength)
                .IsFixedLength()
                .IsRequired();
        });

        builder.Navigation(p => p.Price).IsRequired();

        builder.Property(p => p.StockQuantity)
            .IsRequired();

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.HasIndex(p => p.IsActive);

        builder.Property(p => p.CreatedOnUtc)
            .IsRequired();

        // Recorded events live in memory only; they are never persisted.
        builder.Ignore(p => p.DomainEvents);
    }
}
