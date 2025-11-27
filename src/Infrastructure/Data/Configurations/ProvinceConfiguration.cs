using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class ProvinceConfiguration : IEntityTypeConfiguration<Province>
{
    public void Configure(EntityTypeBuilder<Province> builder)
    {
        builder.ToTable("provinces");
        builder.HasKey(x => x.ProvinceId);

        builder.Property(x => x.ProvinceId)
            .HasColumnName("province_id")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.NameNew)
            .HasColumnName("name_new")
            .HasColumnType("text");

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

        builder.Property(x => x.AdministrativeUnits)
            .HasColumnName("administrative_units")
            .HasColumnType("text");

        builder.Property(x => x.Geometry)
            .HasColumnName("geometry")
            .HasColumnType("geometry(MultiPolygon,4326)");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp without time zone");

        builder.HasMany(x => x.Communes)
            .WithOne(x => x.Province)
            .HasForeignKey(x => x.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

