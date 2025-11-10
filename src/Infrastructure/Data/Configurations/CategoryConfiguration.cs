using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name)
            .HasMaxLength(100)
            .IsRequired();
            
        builder.Property(c => c.Description)
            .HasMaxLength(500);
            
        builder.Property(c => c.CreatedIdUser)
            .HasMaxLength(450);
            
        builder.Property(c => c.UpdatedIdUser)
            .HasMaxLength(450);
            
        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);
        
        // Relationship: 1 Category → Many Products
        builder.HasMany(c => c.Products)
            .WithOne(p => p.Category)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

