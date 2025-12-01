using LotusDharma.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LotusDharma.Infrastructure.Data.Configurations;

public class CommuneConfiguration : IEntityTypeConfiguration<Commune>
{
    public void Configure(EntityTypeBuilder<Commune> builder)
    {
        builder.ToTable("communes");
        builder.HasKey(x => x.CommuneId);

        builder.Property(x => x.CommuneId)
            .HasColumnName("commune_id")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.ProvinceId)
            .HasColumnName("province_id")
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.CommuneCode)
            .HasColumnName("commune_code")
            .HasColumnType("text");

        builder.Property(x => x.Commune3321Id)
            .HasColumnName("commune_3321_id")
            .HasColumnType("text");

        builder.Property(x => x.CommuneInternalId)
            .HasColumnName("commune_internal_id")
            .HasColumnType("text");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.NameNew)
            .HasColumnName("name_new")
            .HasColumnType("text");

        builder.Property(x => x.Type)
            .HasColumnName("type")
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

        builder.Property(x => x.Geometry)
            .HasColumnName("geometry")
            .HasColumnType("geometry(MultiPolygon,4326)");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp without time zone");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp without time zone");

        builder.HasIndex(x => x.ProvinceId)
            .HasDatabaseName("IX_communes_province_id");

        builder.HasOne(x => x.Province)
            .WithMany(x => x.Communes)
            .HasForeignKey(x => x.ProvinceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}



