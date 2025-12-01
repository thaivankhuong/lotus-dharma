using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("provinces");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("province_id")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.AdminCode)
            .HasColumnName("admin_code")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.AdministrativeCenter)
            .HasColumnName("administrative_center")
            .HasColumnType("text");

        builder.Property(x => x.AreaKm2)
            .HasColumnName("area_km2")
            .HasColumnType("double precision");

        builder.Property(x => x.Population)
            .HasColumnName("population")
            .HasColumnType("bigint");

        builder.Property(x => x.Longitude)
            .HasColumnName("longitude")
            .HasColumnType("double precision");

        builder.Property(x => x.Latitude)
            .HasColumnName("latitude")
            .HasColumnType("double precision");

        builder.Property(x => x.BeforeMerger)
            .HasColumnName("before_merger")
            .HasColumnType("text");

        builder.Property(x => x.AdministrativeUnitsInfo)
            .HasColumnName("administrative_units_info")
            .HasColumnType("text");

        builder.Property(x => x.Province34Id)
            .HasColumnName("province_34_id")
            .HasColumnType("text");

        builder.Property(x => x.ProvinceCode)
            .HasColumnName("province_code")
            .HasColumnType("text");

        builder.Property(x => x.Geometry)
            .HasColumnName("geometry")
            .HasColumnType("geometry(MultiPolygon,4326)");

        builder.HasMany(x => x.Communes)
            .WithOne(x => x.Province)
            .HasForeignKey(x => x.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}



