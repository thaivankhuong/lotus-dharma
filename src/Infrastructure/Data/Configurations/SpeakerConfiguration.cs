using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class SpeakerConfiguration : IEntityTypeConfiguration<Speaker>
{
    public void Configure(EntityTypeBuilder<Speaker> builder)
    {
        builder.Property(s => s.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Biography)
            .HasMaxLength(4000);

        builder.Property(s => s.AvatarUrl)
            .HasMaxLength(1000);

        builder.Property(s => s.Slug)
            .HasMaxLength(200);

        builder.HasIndex(s => s.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL");
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.Name);
    }
}
