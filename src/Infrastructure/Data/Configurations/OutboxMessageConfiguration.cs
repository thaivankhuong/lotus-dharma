using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(o => o.Content)
            .IsRequired();

        builder.HasIndex(o => o.ProcessedOn)
            .HasFilter("\"ProcessedOn\" IS NULL")
            .HasDatabaseName("IX_OutboxMessages_Unprocessed");

        builder.HasIndex(o => o.OccurredOn);
    }
}
