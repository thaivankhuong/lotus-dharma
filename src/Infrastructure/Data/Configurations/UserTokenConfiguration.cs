using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class UserTokenConfiguration : IEntityTypeConfiguration<UserToken>
{
    public void Configure(EntityTypeBuilder<UserToken> builder)
    {
        builder.ToTable("UserTokens");

        builder.Property(ut => ut.Token)
            .IsRequired();

        builder.Property(ut => ut.RefreshToken)
            .HasMaxLength(500);

        builder.Property(ut => ut.IpAddress)
            .HasMaxLength(50);

        builder.Property(ut => ut.UserAgent)
            .HasMaxLength(500);

        // Index để tìm kiếm token nhanh
        builder.HasIndex(ut => ut.Token);
        builder.HasIndex(ut => ut.RefreshToken);
        builder.HasIndex(ut => ut.UserId);
    }
}

