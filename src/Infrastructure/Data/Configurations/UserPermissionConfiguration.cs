using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");

        // Composite primary key
        builder.HasKey(up => new { up.UserId, up.PermissionId });

        // Default value for IsGranted (true for granted permissions)
        builder.Property(up => up.IsGranted)
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(up => up.User)
            .WithMany(u => u.UserPermissions)
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(up => up.Permission)
            .WithMany(p => p.UserPermissions)
            .HasForeignKey(up => up.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index to prevent duplicate user-permission assignments
        builder.HasIndex(up => new { up.UserId, up.PermissionId })
            .IsUnique();
    }
}