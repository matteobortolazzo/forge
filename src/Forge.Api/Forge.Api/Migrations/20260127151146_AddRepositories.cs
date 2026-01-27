using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRepositories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TaskLogs_TaskId",
                table: "TaskLogs");

            migrationBuilder.AddColumn<Guid>(
                name: "RepositoryId",
                table: "Tasks",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CommitHash = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RemoteUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsDirty = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsGitRepository = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastRefreshedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_RepositoryId",
                table: "Tasks",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TaskLogs_Task_Timestamp",
                table: "TaskLogs",
                columns: new[] { "TaskId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Default_Active",
                table: "Repositories",
                columns: new[] { "IsDefault", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Path",
                table: "Repositories",
                column: "Path",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Repositories_RepositoryId",
                table: "Tasks",
                column: "RepositoryId",
                principalTable: "Repositories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Repositories_RepositoryId",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "Repositories");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_RepositoryId",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_TaskLogs_Task_Timestamp",
                table: "TaskLogs");

            migrationBuilder.DropColumn(
                name: "RepositoryId",
                table: "Tasks");

            migrationBuilder.CreateIndex(
                name: "IX_TaskLogs_TaskId",
                table: "TaskLogs",
                column: "TaskId");
        }
    }
}
