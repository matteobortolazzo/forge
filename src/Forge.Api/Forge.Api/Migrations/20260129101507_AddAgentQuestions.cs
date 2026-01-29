using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAgentQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AgentQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BacklogItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ToolUseId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    QuestionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TimeoutAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AnswersJson = table.Column<string>(type: "TEXT", nullable: true),
                    AnsweredAt = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentQuestions_BacklogItems_BacklogItemId",
                        column: x => x.BacklogItemId,
                        principalTable: "BacklogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentQuestions_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentQuestions_BacklogItem_Status",
                table: "AgentQuestions",
                columns: new[] { "BacklogItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentQuestions_Task_RequestedAt",
                table: "AgentQuestions",
                columns: new[] { "TaskId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentQuestions_Task_Status",
                table: "AgentQuestions",
                columns: new[] { "TaskId", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentQuestions");
        }
    }
}
