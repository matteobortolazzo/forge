using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Api.Migrations
{
    /// <inheritdoc />
    public partial class PendingModelChanges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SimplificationIterations",
                table: "Tasks");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SimplificationIterations",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
