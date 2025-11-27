using Microsoft.EntityFrameworkCore.Migrations;

namespace LotusDharma.Infrastructure.Migrations;

public partial class AddProvincesAndCommunes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS provinces (
                province_id text PRIMARY KEY,
                name text NOT NULL,
                name_new text,
                administrative_center text,
                area_km2 double precision,
                population bigint,
                longitude double precision,
                latitude double precision,
                before_merger text,
                administrative_units text,
                geometry geometry(MultiPolygon,4326),
                created_at timestamp without time zone,
                updated_at timestamp without time zone
            );
        ");

        migrationBuilder.Sql(@"
            CREATE TABLE IF NOT EXISTS communes (
                commune_id text PRIMARY KEY,
                province_id text NOT NULL,
                name text NOT NULL,
                name_new text,
                type text,
                area_km2 double precision,
                population bigint,
                longitude double precision,
                latitude double precision,
                before_merger text,
                geometry geometry(MultiPolygon,4326),
                created_at timestamp without time zone,
                updated_at timestamp without time zone,
                CONSTRAINT fk_communes_provinces FOREIGN KEY (province_id) REFERENCES provinces(province_id)
            );
        ");

        migrationBuilder.Sql(@"
            CREATE INDEX IF NOT EXISTS IX_communes_province_id ON communes(province_id);
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Intentionally empty: do not drop production geography tables.
    }
}

