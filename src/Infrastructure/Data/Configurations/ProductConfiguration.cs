using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(1000);

        builder.Property(p => p.Price)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.Property(p => p.Stock)
            .IsRequired();
            
        builder.Property(p => p.CategoryId)
            .IsRequired();
            
        builder.Property(p => p.CreatedIdUser)
            .HasMaxLength(450);

        builder.Property(p => p.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
            
        // Index for FK
        builder.HasIndex(p => p.CategoryId);
    }
}
