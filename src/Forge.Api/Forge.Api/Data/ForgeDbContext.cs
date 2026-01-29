using Forge.Api.Data.Entities;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Data;

public class ForgeDbContext(DbContextOptions<ForgeDbContext> options) : DbContext(options)
{
    public DbSet<RepositoryEntity> Repositories => Set<RepositoryEntity>();
    public DbSet<BacklogItemEntity> BacklogItems => Set<BacklogItemEntity>();
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<TaskLogEntity> TaskLogs => Set<TaskLogEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<AgentArtifactEntity> AgentArtifacts => Set<AgentArtifactEntity>();
    public DbSet<HumanGateEntity> HumanGates => Set<HumanGateEntity>();
    public DbSet<RollbackRecordEntity> RollbackRecords => Set<RollbackRecordEntity>();
    public DbSet<AgentQuestionEntity> AgentQuestions => Set<AgentQuestionEntity>();

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

            entity.HasMany(e => e.BacklogItems)
                .WithOne(b => b.Repository)
                .HasForeignKey(b => b.RepositoryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BacklogItemEntity>(entity =>
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

            // Context detection fields
            entity.Property(e => e.DetectedLanguage).HasMaxLength(50);
            entity.Property(e => e.DetectedFramework).HasMaxLength(50);

            // Confidence and human gate tracking
            entity.Property(e => e.ConfidenceScore).HasPrecision(3, 2);
            entity.Property(e => e.HumanInputReason).HasMaxLength(1000);

            // Index for scheduler queries (schedulable backlog items)
            entity.HasIndex(e => new { e.State, e.IsPaused, e.AssignedAgentId })
                .HasDatabaseName("IX_BacklogItems_Schedulable");

            // Index for repository queries
            entity.HasIndex(e => e.RepositoryId)
                .HasDatabaseName("IX_BacklogItems_RepositoryId");

            entity.HasMany(e => e.Tasks)
                .WithOne(t => t.BacklogItem)
                .HasForeignKey(t => t.BacklogItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Artifacts)
                .WithOne(a => a.BacklogItem)
                .HasForeignKey(a => a.BacklogItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.HumanGates)
                .WithOne(g => g.BacklogItem)
                .HasForeignKey(g => g.BacklogItemId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Logs)
                .WithOne(l => l.BacklogItem)
                .HasForeignKey(l => l.BacklogItemId)
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

            // Index for scheduler queries (schedulable tasks)
            entity.HasIndex(e => new { e.State, e.IsPaused, e.AssignedAgentId })
                .HasDatabaseName("IX_Tasks_Schedulable");

            // Index for backlog item queries
            entity.HasIndex(e => new { e.BacklogItemId, e.ExecutionOrder })
                .HasDatabaseName("IX_Tasks_BacklogItem_Order");

            // Index for repository queries
            entity.HasIndex(e => e.RepositoryId)
                .HasDatabaseName("IX_Tasks_RepositoryId");

            // Agent context detection fields
            entity.Property(e => e.DetectedLanguage).HasMaxLength(50);
            entity.Property(e => e.DetectedFramework).HasMaxLength(50);
            entity.Property(e => e.RecommendedNextState)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Confidence and human gate tracking
            entity.Property(e => e.ConfidenceScore).HasPrecision(3, 2);
            entity.Property(e => e.HumanInputReason).HasMaxLength(1000);

            entity.HasMany(e => e.Logs)
                .WithOne(l => l.Task)
                .HasForeignKey(l => l.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Artifacts)
                .WithOne(a => a.Task)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.HumanGates)
                .WithOne(g => g.Task)
                .HasForeignKey(g => g.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            // Repository relationship (through BacklogItem, but also direct FK for queries)
            entity.HasOne(e => e.Repository)
                .WithMany()
                .HasForeignKey(e => e.RepositoryId)
                .OnDelete(DeleteBehavior.Restrict);  // Prevent cascade conflict
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

            // Index for retrieving logs by backlog item (ordered by timestamp)
            entity.HasIndex(e => new { e.BacklogItemId, e.Timestamp })
                .HasDatabaseName("IX_TaskLogs_BacklogItem_Timestamp");
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
            entity.Property(e => e.ProducedInBacklogState)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.ArtifactType)
                .HasConversion<string>()
                .HasMaxLength(30);
            entity.Property(e => e.AgentId).HasMaxLength(100);

            // Index for retrieving artifacts by task (ordered by creation)
            entity.HasIndex(e => new { e.TaskId, e.CreatedAt })
                .HasDatabaseName("IX_AgentArtifacts_Task_CreatedAt");

            // Index for retrieving artifacts by state
            entity.HasIndex(e => new { e.TaskId, e.ProducedInState })
                .HasDatabaseName("IX_AgentArtifacts_Task_State");

            // Index for retrieving artifacts by backlog item (ordered by creation)
            entity.HasIndex(e => new { e.BacklogItemId, e.CreatedAt })
                .HasDatabaseName("IX_AgentArtifacts_BacklogItem_CreatedAt");

            entity.Property(e => e.HumanInputReason).HasMaxLength(1000);
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

            // Index for finding pending gates by task
            entity.HasIndex(e => new { e.TaskId, e.Status })
                .HasDatabaseName("IX_HumanGates_Task_Status");

            // Index for finding pending gates by backlog item
            entity.HasIndex(e => new { e.BacklogItemId, e.Status })
                .HasDatabaseName("IX_HumanGates_BacklogItem_Status");
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
        });

        modelBuilder.Entity<AgentQuestionEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ToolUseId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.QuestionsJson).IsRequired();
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Index for finding pending questions by task
            entity.HasIndex(e => new { e.TaskId, e.Status })
                .HasDatabaseName("IX_AgentQuestions_Task_Status");

            // Index for finding pending questions by backlog item
            entity.HasIndex(e => new { e.BacklogItemId, e.Status })
                .HasDatabaseName("IX_AgentQuestions_BacklogItem_Status");

            // Index for finding questions by timestamp (ordering)
            entity.HasIndex(e => new { e.TaskId, e.RequestedAt })
                .HasDatabaseName("IX_AgentQuestions_Task_RequestedAt");

            entity.HasOne(e => e.Task)
                .WithMany()
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.BacklogItem)
                .WithMany()
                .HasForeignKey(e => e.BacklogItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
