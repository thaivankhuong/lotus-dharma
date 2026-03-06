using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class DharmaTalkConfiguration : IEntityTypeConfiguration<DharmaTalk>
{
    public void Configure(EntityTypeBuilder<DharmaTalk> builder)
    {
        builder.Property(t => t.Title)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(4000);

        builder.Property(t => t.Slug)
            .HasMaxLength(500);

        builder.Property(t => t.AudioBlobName)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(t => t.AudioUrl)
            .HasMaxLength(2000);

        builder.Property(t => t.AudioContentType)
            .HasMaxLength(100)
            .HasDefaultValue("audio/mpeg");

        builder.Property(t => t.CoverImageUrl)
            .HasMaxLength(1000);

        builder.Property(t => t.Tags)
            .HasMaxLength(1000);

        builder.HasIndex(t => t.Slug).IsUnique().HasFilter("\"Slug\" IS NOT NULL");
        builder.HasIndex(t => t.SpeakerId);
        builder.HasIndex(t => t.SeriesId);
        builder.HasIndex(t => t.IsPublished);
        builder.HasIndex(t => t.PublishedAt);
        builder.HasIndex(t => new { t.IsPublished, t.PublishedAt })
            .HasDatabaseName("IX_DharmaTalks_Published_Date");

        builder.HasOne(t => t.Speaker)
            .WithMany(s => s.DharmaTalks)
            .HasForeignKey(t => t.SpeakerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Series)
            .WithMany(s => s.DharmaTalks)
            .HasForeignKey(t => t.SeriesId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
