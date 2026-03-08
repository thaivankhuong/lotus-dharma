using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotusDharma.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserProviderIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Users_Provider_ProviderId",
                table: "Users",
                columns: new[] { "Provider", "ProviderId" },
                filter: "\"Provider\" IS NOT NULL AND \"ProviderId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_Provider_ProviderId",
                table: "Users");
        }
    }
}
