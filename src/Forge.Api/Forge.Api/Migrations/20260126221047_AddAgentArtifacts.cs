using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentArtifacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DetectedFramework",
                table: "Tasks",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DetectedLanguage",
                table: "Tasks",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecommendedNextState",
                table: "Tasks",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AgentArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProducedInState = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ArtifactType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentArtifacts_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentArtifacts_Task_CreatedAt",
                table: "AgentArtifacts",
                columns: new[] { "TaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentArtifacts_Task_State",
                table: "AgentArtifacts",
                columns: new[] { "TaskId", "ProducedInState" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentArtifacts");

            migrationBuilder.DropColumn(
                name: "DetectedFramework",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "DetectedLanguage",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "RecommendedNextState",
                table: "Tasks");
        }
    }
}
