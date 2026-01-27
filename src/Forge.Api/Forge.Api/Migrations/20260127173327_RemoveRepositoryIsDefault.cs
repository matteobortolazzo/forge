using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Api.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRepositoryIsDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Repositories_Default_Active",
                table: "Repositories");

            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Repositories");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Active",
                table: "Repositories",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Repositories_Active",
                table: "Repositories");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Repositories",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Default_Active",
                table: "Repositories",
                columns: new[] { "IsDefault", "IsActive" });
        }
    }
}
