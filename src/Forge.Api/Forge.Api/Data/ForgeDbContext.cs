using Forge.Api.Data.Entities;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Data;

public class ForgeDbContext(DbContextOptions<ForgeDbContext> options) : DbContext(options)
{
    public DbSet<RepositoryEntity> Repositories => Set<RepositoryEntity>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<TaskLogEntity> TaskLogs => Set<TaskLogEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<AgentArtifactEntity> AgentArtifacts => Set<AgentArtifactEntity>();
    public DbSet<SubtaskEntity> Subtasks => Set<SubtaskEntity>();
    public DbSet<HumanGateEntity> HumanGates => Set<HumanGateEntity>();
    public DbSet<RollbackRecordEntity> RollbackRecords => Set<RollbackRecordEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<RepositoryEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Path).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Branch).HasMaxLength(200);
            entity.Property(e => e.CommitHash).HasMaxLength(50);
            entity.Property(e => e.RemoteUrl).HasMaxLength(500);

            // Unique constraint on Path
            entity.HasIndex(e => e.Path)
                .IsUnique()
                .HasDatabaseName("IX_Repositories_Path");

            // Index for queries on IsActive
            entity.HasIndex(e => e.IsActive)
                .HasDatabaseName("IX_Repositories_Active");

            entity.HasMany(e => e.Tasks)
                .WithOne(t => t.Repository)
                .HasForeignKey(t => t.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.State)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.Priority)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.AssignedAgentId).HasMaxLength(100);
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.PauseReason).HasMaxLength(500);
            entity.Property(e => e.DerivedState)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Index for scheduler queries (schedulable tasks)
            entity.HasIndex(e => new { e.State, e.IsPaused, e.AssignedAgentId })
                .HasDatabaseName("IX_Tasks_Schedulable");

            // Index for hierarchy queries (children by parent)
            entity.HasIndex(e => e.ParentId)
                .HasDatabaseName("IX_Tasks_ParentId");

            // Index for repository queries
            entity.HasIndex(e => e.RepositoryId)
                .HasDatabaseName("IX_Tasks_RepositoryId");

            // Self-referential relationship for task hierarchy
            entity.HasOne(e => e.Parent)
                .WithMany(e => e.Children)
                .HasForeignKey(e => e.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Logs)
                .WithOne(l => l.Task)
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Artifacts)
                .WithOne(a => a.Task)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Agent context detection fields
            entity.Property(e => e.DetectedLanguage).HasMaxLength(50);
            entity.Property(e => e.DetectedFramework).HasMaxLength(50);
            entity.Property(e => e.RecommendedNextState)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Confidence and human gate tracking
            entity.Property(e => e.ConfidenceScore).HasPrecision(3, 2);
            entity.Property(e => e.HumanInputReason).HasMaxLength(1000);

            // Subtasks relationship
            entity.HasMany(e => e.Subtasks)
                .WithOne(s => s.ParentTask)
                .HasForeignKey(s => s.ParentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Human gates relationship
            entity.HasMany(e => e.HumanGates)
                .WithOne(g => g.Task)
                .HasForeignKey(g => g.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TaskLogEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ToolName).HasMaxLength(100);

            // Index for retrieving logs by task (ordered by timestamp)
            entity.HasIndex(e => new { e.TaskId, e.Timestamp })
                .HasDatabaseName("IX_TaskLogs_Task_Timestamp");
        });

        modelBuilder.Entity<NotificationEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Message).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.Read);

            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<AgentArtifactEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.ProducedInState)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.ArtifactType)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.AgentId).HasMaxLength(100);

            // Index for retrieving artifacts by task (ordered by creation)
            entity.HasIndex(e => new { e.TaskId, e.CreatedAt })
                .HasDatabaseName("IX_AgentArtifacts_Task_CreatedAt");

            // Index for retrieving artifacts by state
            entity.HasIndex(e => new { e.TaskId, e.ProducedInState })
                .HasDatabaseName("IX_AgentArtifacts_Task_State");

            // Index for retrieving artifacts by subtask
            entity.HasIndex(e => new { e.SubtaskId, e.CreatedAt })
                .HasDatabaseName("IX_AgentArtifacts_Subtask_CreatedAt");

            entity.Property(e => e.HumanInputReason).HasMaxLength(1000);
        });

        modelBuilder.Entity<SubtaskEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.EstimatedScope)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.CurrentStage)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.WorktreePath).HasMaxLength(500);
            entity.Property(e => e.BranchName).HasMaxLength(200);
            entity.Property(e => e.FailureReason).HasMaxLength(2000);
            entity.Property(e => e.ConfidenceScore).HasPrecision(3, 2);

            // Index for retrieving subtasks by parent task
            entity.HasIndex(e => new { e.ParentTaskId, e.ExecutionOrder })
                .HasDatabaseName("IX_Subtasks_Parent_Order");

            // Index for finding pending subtasks
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_Subtasks_Status");

            entity.HasOne(e => e.ParentTask)
                .WithMany(t => t.Subtasks)
                .HasForeignKey(e => e.ParentTaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Artifacts)
                .WithOne(a => a.Subtask)
                .HasForeignKey(a => a.SubtaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<HumanGateEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GateType)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.ConfidenceScore).HasPrecision(3, 2);
            entity.Property(e => e.Reason).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ResolvedBy).HasMaxLength(200);
            entity.Property(e => e.Resolution).HasMaxLength(2000);

            // Index for finding pending gates
            entity.HasIndex(e => new { e.TaskId, e.Status })
                .HasDatabaseName("IX_HumanGates_Task_Status");

            entity.HasOne(e => e.Task)
                .WithMany(t => t.HumanGates)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Subtask)
                .WithMany(s => s.HumanGates)
                .HasForeignKey(e => e.SubtaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<RollbackRecordEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Trigger)
                .HasConversion<string>()
                .HasMaxLength(30);
            entity.Property(e => e.Notes).HasMaxLength(2000);

            // Index for audit trail
            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_RollbackRecords_Timestamp");

            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.Subtask)
                .WithMany()
                .HasForeignKey(e => e.SubtaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
