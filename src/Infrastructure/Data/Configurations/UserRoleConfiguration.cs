using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("UserRoles");

        // Composite index để ngăn chặn duplicate user-role assignments
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique();
    }
}

