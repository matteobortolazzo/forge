using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ChildCount",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DerivedState",
                table: "Tasks",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentId",
                table: "Tasks",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ParentId",
                table: "Tasks",
                column: "ParentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Tasks_ParentId",
                table: "Tasks",
                column: "ParentId",
                principalTable: "Tasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Tasks_ParentId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ParentId",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ChildCount",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DerivedState",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ParentId",
                table: "Tasks");
        }
    }
}
