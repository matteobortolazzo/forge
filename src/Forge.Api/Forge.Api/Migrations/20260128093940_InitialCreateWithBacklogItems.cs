using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Api.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateWithBacklogItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Repositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Path = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Branch = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CommitHash = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RemoteUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    IsDirty = table.Column<bool>(type: "INTEGER", nullable: true),
                    IsGitRepository = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastRefreshedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Repositories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BacklogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AcceptanceCriteria = table.Column<string>(type: "TEXT", nullable: true),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DetectedLanguage = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DetectedFramework = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: true),
                    HumanInputRequested = table.Column<bool>(type: "INTEGER", nullable: false),
                    HumanInputReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    HasPendingGate = table.Column<bool>(type: "INTEGER", nullable: false),
                    RefiningIterations = table.Column<int>(type: "INTEGER", nullable: false),
                    AssignedAgentId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HasError = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    IsPaused = table.Column<bool>(type: "INTEGER", nullable: false),
                    PauseReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PausedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxRetries = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    TaskCount = table.Column<int>(type: "INTEGER", nullable: false),
                    CompletedTaskCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BacklogItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BacklogItems_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tasks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    State = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Priority = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    AssignedAgentId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    HasError = table.Column<bool>(type: "INTEGER", nullable: false),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    BacklogItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ExecutionOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsPaused = table.Column<bool>(type: "INTEGER", nullable: false),
                    PauseReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    PausedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    RetryCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxRetries = table.Column<int>(type: "INTEGER", nullable: false),
                    DetectedLanguage = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DetectedFramework = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    RecommendedNextState = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "TEXT", precision: 3, scale: 2, nullable: true),
                    HumanInputRequested = table.Column<bool>(type: "INTEGER", nullable: false),
                    HumanInputReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    HasPendingGate = table.Column<bool>(type: "INTEGER", nullable: false),
                    ImplementationRetries = table.Column<int>(type: "INTEGER", nullable: false),
                    SimplificationIterations = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tasks_BacklogItems_BacklogItemId",
                        column: x => x.BacklogItemId,
                        principalTable: "BacklogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Tasks_Repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "Repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AgentArtifacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BacklogItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ProducedInState = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ProducedInBacklogState = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ArtifactType = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AgentId = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ConfidenceScore = table.Column<decimal>(type: "TEXT", nullable: true),
                    HumanInputRequested = table.Column<bool>(type: "INTEGER", nullable: false),
                    HumanInputReason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgentArtifacts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgentArtifacts_BacklogItems_BacklogItemId",
                        column: x => x.BacklogItemId,
                        principalTable: "BacklogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AgentArtifacts_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HumanGates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BacklogItemId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                        name: "FK_HumanGates_BacklogItems_BacklogItemId",
                        column: x => x.BacklogItemId,
                        principalTable: "BacklogItems",
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
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BacklogItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Read = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_BacklogItems_BacklogItemId",
                        column: x => x.BacklogItemId,
                        principalTable: "BacklogItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Notifications_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RollbackRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BacklogItemId = table.Column<Guid>(type: "TEXT", nullable: true),
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
                        name: "FK_RollbackRecords_BacklogItems_BacklogItemId",
                        column: x => x.BacklogItemId,
                        principalTable: "BacklogItems",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RollbackRecords_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TaskLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TaskId = table.Column<Guid>(type: "TEXT", nullable: true),
                    BacklogItemId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Type = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "TEXT", nullable: false),
                    ToolName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TaskLogs_BacklogItems_BacklogItemId",
                        column: x => x.BacklogItemId,
                        principalTable: "BacklogItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TaskLogs_Tasks_TaskId",
                        column: x => x.TaskId,
                        principalTable: "Tasks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgentArtifacts_BacklogItem_CreatedAt",
                table: "AgentArtifacts",
                columns: new[] { "BacklogItemId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentArtifacts_Task_CreatedAt",
                table: "AgentArtifacts",
                columns: new[] { "TaskId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AgentArtifacts_Task_State",
                table: "AgentArtifacts",
                columns: new[] { "TaskId", "ProducedInState" });

            migrationBuilder.CreateIndex(
                name: "IX_BacklogItems_RepositoryId",
                table: "BacklogItems",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BacklogItems_Schedulable",
                table: "BacklogItems",
                columns: new[] { "State", "IsPaused", "AssignedAgentId" });

            migrationBuilder.CreateIndex(
                name: "IX_HumanGates_BacklogItem_Status",
                table: "HumanGates",
                columns: new[] { "BacklogItemId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_HumanGates_Task_Status",
                table: "HumanGates",
                columns: new[] { "TaskId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_BacklogItemId",
                table: "Notifications",
                column: "BacklogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CreatedAt",
                table: "Notifications",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Read",
                table: "Notifications",
                column: "Read");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TaskId",
                table: "Notifications",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Active",
                table: "Repositories",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Repositories_Path",
                table: "Repositories",
                column: "Path",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RollbackRecords_BacklogItemId",
                table: "RollbackRecords",
                column: "BacklogItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RollbackRecords_TaskId",
                table: "RollbackRecords",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_RollbackRecords_Timestamp",
                table: "RollbackRecords",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TaskLogs_BacklogItem_Timestamp",
                table: "TaskLogs",
                columns: new[] { "BacklogItemId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TaskLogs_Task_Timestamp",
                table: "TaskLogs",
                columns: new[] { "TaskId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_BacklogItem_Order",
                table: "Tasks",
                columns: new[] { "BacklogItemId", "ExecutionOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_RepositoryId",
                table: "Tasks",
                column: "RepositoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Schedulable",
                table: "Tasks",
                columns: new[] { "State", "IsPaused", "AssignedAgentId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgentArtifacts");

            migrationBuilder.DropTable(
                name: "HumanGates");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "RollbackRecords");

            migrationBuilder.DropTable(
                name: "TaskLogs");

            migrationBuilder.DropTable(
                name: "Tasks");

            migrationBuilder.DropTable(
                name: "BacklogItems");

            migrationBuilder.DropTable(
                name: "Repositories");
        }
    }
}
