using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPipelineEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ConfidenceScore",
                table: "Tasks",
                type: "TEXT",
                precision: 3,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasPendingGate",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "HumanInputReason",
                table: "Tasks",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HumanInputRequested",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ImplementationRetries",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SimplificationIterations",
                table: "Tasks",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ConfidenceScore",
                table: "AgentArtifacts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HumanInputReason",
                table: "AgentArtifacts",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HumanInputRequested",
                table: "AgentArtifacts",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SubtaskId",
                table: "AgentArtifacts",
                type: "TEXT",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Subtasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ParentTaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    AcceptanceCriteriaJson = table.Column<string>(type: "TEXT", nullable: false),
                    EstimatedScope = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    DependenciesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    WorktreePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BranchName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: true),
                    CurrentStage = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ImplementationRetries = table.Column<int>(type: "INTEGER", nullable: false),
                    SimplificationIterations = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FailureReason = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subtasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subtasks_Tasks_ParentTaskId",
                        column: x => x.ParentTaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HumanGates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SubtaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    GateType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ResolvedBy = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Resolution = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    ContextJson = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HumanGates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HumanGates_Subtasks_SubtaskId",
                        column: x => x.SubtaskId,
                        principalTable: "Subtasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_HumanGates_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RollbackRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    SubtaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Trigger = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StateBeforeJson = table.Column<string>(type: "TEXT", nullable: false),
                    ActionTakenJson = table.Column<string>(type: "TEXT", nullable: false),
                    PreservedArtifactsJson = table.Column<string>(type: "TEXT", nullable: false),
                    RecoveryOptionsJson = table.Column<string>(type: "TEXT", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RollbackRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RollbackRecords_Subtasks_SubtaskId",
                        column: x => x.SubtaskId,
                        principalTable: "Subtasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RollbackRecords_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentArtifacts_Subtask_CreatedAt",
                table: "AgentArtifacts",
                columns: new[] { "SubtaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_HumanGates_SubtaskId",
                table: "HumanGates",
                column: "SubtaskId");

            migrationBuilder.CreateIndex(
                name: "IX_HumanGates_Task_Status",
                table: "HumanGates",
                columns: new[] { "TaskId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_RollbackRecords_SubtaskId",
                table: "RollbackRecords",
                column: "SubtaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RollbackRecords_TaskId",
                table: "RollbackRecords",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RollbackRecords_Timestamp",
                table: "RollbackRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_Parent_Order",
                table: "Subtasks",
                columns: new[] { "ParentTaskId", "ExecutionOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Subtasks_Status",
                table: "Subtasks",
                column: "Status");

            migrationBuilder.AddForeignKey(
                name: "FK_AgentArtifacts_Subtasks_SubtaskId",
                table: "AgentArtifacts",
                column: "SubtaskId",
                principalTable: "Subtasks",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AgentArtifacts_Subtasks_SubtaskId",
                table: "AgentArtifacts");

            migrationBuilder.DropTable(
                name: "HumanGates");

            migrationBuilder.DropTable(
                name: "RollbackRecords");

            migrationBuilder.DropTable(
                name: "Subtasks");

            migrationBuilder.DropIndex(
                name: "IX_AgentArtifacts_Subtask_CreatedAt",
                table: "AgentArtifacts");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "HasPendingGate",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "HumanInputReason",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "HumanInputRequested",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ImplementationRetries",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SimplificationIterations",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ConfidenceScore",
                table: "AgentArtifacts");

            migrationBuilder.DropColumn(
                name: "HumanInputReason",
                table: "AgentArtifacts");

            migrationBuilder.DropColumn(
                name: "HumanInputRequested",
                table: "AgentArtifacts");

            migrationBuilder.DropColumn(
                name: "SubtaskId",
                table: "AgentArtifacts");
        }
    }
}
