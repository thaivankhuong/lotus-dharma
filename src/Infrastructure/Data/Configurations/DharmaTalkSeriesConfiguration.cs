using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class DharmaTalkSeriesConfiguration : IEntityTypeConfiguration<DharmaTalkSeries>
{
    public void Configure(EntityTypeBuilder<DharmaTalkSeries> builder)
    {
        builder.ToTable("DharmaTalkSeries");

        builder.Property(s => s.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasMaxLength(4000);

        builder.Property(s => s.CoverImageUrl)
            .HasMaxLength(1000);

        builder.Property(s => s.Slug)
            .HasMaxLength(500);

        builder.HasIndex(s => s.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL");
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.SortOrder);
    }
}
