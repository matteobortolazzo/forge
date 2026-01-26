using Forge.Api.Data.Entities;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Data;

public class ForgeDbContext(DbContextOptions<ForgeDbContext> options) : DbContext(options)
{
    public DbSet<TaskEntity> Tasks => Set<TaskEntity>();
    public DbSet<TaskLogEntity> TaskLogs => Set<TaskLogEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

            entity.HasMany(e => e.Logs)
                .WithOne(l => l.Task)
                .HasForeignKey(l => l.TaskId)
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
    }
}
