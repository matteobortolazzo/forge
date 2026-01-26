using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSchedulingFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsPaused",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MaxRetries",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "PauseReason",
                table: "Tasks",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PausedAt",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Schedulable",
                table: "Tasks",
                columns: new[] { "State", "IsPaused", "AssignedAgentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_Schedulable",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IsPaused",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "MaxRetries",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PauseReason",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "PausedAt",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "Tasks");
        }
    }
}
