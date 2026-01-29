using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Agent;
using Forge.Api.Features.Events;
using Forge.Api.Features.HumanGates;
using Forge.Api.Features.Notifications;
using Forge.Api.Features.Scheduler;
using Forge.Api.Features.Tasks;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forge.Api.UnitTests.Features.Scheduler;

public class SchedulerServiceTests : IDisposable
{
    private readonly ForgeDbContext _db;
    private readonly ISseService _sseMock;
    private readonly NotificationService _notificationService;
    private readonly SchedulerState _schedulerState;
    private readonly HumanGateService _humanGateService;
    private readonly SchedulerService _sut;

    public SchedulerServiceTests()
    {
        var options = new DbContextOptionsBuilder<ForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ForgeDbContext(options);

        _sseMock = Substitute.For<ISseService>();
        _notificationService = new NotificationService(_db, _sseMock);
        _schedulerState = new SchedulerState();

        var schedulerOptions = Options.Create(new SchedulerOptions
        {
            DefaultMaxRetries = 3
        });

        var pipelineConfig = Options.Create(new PipelineConfiguration
        {
            MaxImplementationRetries = 3,
            MaxSimplificationIterations = 2,
            ConfidenceThreshold = 0.7m
        });

        var humanGateLoggerMock = Substitute.For<ILogger<HumanGateService>>();
        _humanGateService = new HumanGateService(_db, _sseMock, pipelineConfig, humanGateLoggerMock);

        var loggerMock = Substitute.For<ILogger<SchedulerService>>();

        _sut = new SchedulerService(
            _db,
            _sseMock,
            _notificationService,
            _schedulerState,
            schedulerOptions,
            pipelineConfig,
            _humanGateService,
            loggerMock);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    #region GetNextSchedulableTaskAsync

    [Fact]
    public async Task GetNextSchedulableTask_ReturnsHighestPriorityTask()
    {
        // Arrange
        await CreateTaskAsync("Low priority", Priority.Low, PipelineState.Planning);
        await CreateTaskAsync("High priority", Priority.High, PipelineState.Planning);
        await CreateTaskAsync("Medium priority", Priority.Medium, PipelineState.Planning);

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("High priority");
    }

    [Fact]
    public async Task GetNextSchedulableTask_WithSamePriority_OrdersByState_PlanningFirst()
    {
        // Arrange
        await CreateTaskAsync("Implementing task", Priority.High, PipelineState.Implementing);
        await CreateTaskAsync("Planning task", Priority.High, PipelineState.Planning);

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Planning task");
    }

    [Fact]
    public async Task GetNextSchedulableTask_ExcludesPausedTasks()
    {
        // Arrange
        await CreateTaskAsync("Paused task", Priority.High, PipelineState.Planning, isPaused: true);
        await CreateTaskAsync("Available task", Priority.Low, PipelineState.Planning);

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Available task");
    }

    [Fact]
    public async Task GetNextSchedulableTask_ExcludesTasksExceedingMaxRetries()
    {
        // Arrange
        await CreateTaskAsync("Exceeded retries", Priority.High, PipelineState.Planning, hasError: true, retryCount: 3, maxRetries: 3);
        await CreateTaskAsync("Available task", Priority.Low, PipelineState.Planning);

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Available task");
    }

    [Fact]
    public async Task GetNextSchedulableTask_IncludesErroredTasksWithRetriesRemaining()
    {
        // Arrange
        await CreateTaskAsync("Error with retries left", Priority.High, PipelineState.Planning, hasError: true, retryCount: 1, maxRetries: 3);

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Error with retries left");
    }

    [Fact]
    public async Task GetNextSchedulableTask_ExcludesTasksWithAssignedAgent()
    {
        // Arrange
        await CreateTaskAsync("Already assigned", Priority.High, PipelineState.Planning, assignedAgentId: "claude-agent");
        await CreateTaskAsync("Not assigned", Priority.Low, PipelineState.Planning);

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Not assigned");
    }

    [Theory]
    [InlineData(PipelineState.PrReady)]
    public async Task GetNextSchedulableTask_ExcludesNonSchedulableStates(PipelineState state)
    {
        // Arrange
        await CreateTaskAsync("Non-schedulable", Priority.High, state);
        await CreateTaskAsync("Schedulable", Priority.Low, PipelineState.Planning);

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Schedulable");
    }

    [Theory]
    [InlineData(PipelineState.Planning)]
    [InlineData(PipelineState.Implementing)]
    public async Task GetNextSchedulableTask_IncludesSchedulableStates(PipelineState state)
    {
        // Arrange
        await CreateTaskAsync("Schedulable task", Priority.High, state);

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Schedulable task");
    }

    [Fact]
    public async Task GetNextSchedulableTask_ReturnsNullWhenDisabled()
    {
        // Arrange
        await CreateTaskAsync("Available task", Priority.High, PipelineState.Planning);
        _sut.Disable();

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNextSchedulableTask_ReturnsNullWhenNoTasksAvailable()
    {
        // Arrange - no tasks

        // Act
        var result = await _sut.GetNextSchedulableTaskAsync();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region HandleAgentCompletionAsync

    [Theory]
    [InlineData(PipelineState.Planning, PipelineState.Implementing)]
    [InlineData(PipelineState.Implementing, PipelineState.PrReady)]
    public async Task HandleAgentCompletion_OnSuccess_TransitionsToCorrectNextState(PipelineState from, PipelineState to)
    {
        // Arrange
        var taskId = await CreateTaskAsync("Test task", Priority.Medium, from);

        // Act
        var result = await _sut.HandleAgentCompletionAsync(taskId, AgentCompletionResult.Success);

        // Assert
        result.Should().NotBeNull();
        result!.State.Should().Be(to);
    }

    [Fact]
    public async Task HandleAgentCompletion_OnSuccess_EmitsSseEvent()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Test task", Priority.Medium, PipelineState.Planning);

        // Act
        await _sut.HandleAgentCompletionAsync(taskId, AgentCompletionResult.Success);

        // Assert
        await _sseMock.Received(1).EmitTaskUpdatedAsync(Arg.Is<TaskDto>(t => t.State == PipelineState.Implementing));
    }

    [Fact]
    public async Task HandleAgentCompletion_OnSuccess_ClearsErrorState()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Error task", Priority.Medium, PipelineState.Planning, hasError: true, retryCount: 2);

        // Act
        var result = await _sut.HandleAgentCompletionAsync(taskId, AgentCompletionResult.Success);

        // Assert
        result!.HasError.Should().BeFalse();
        result.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAgentCompletion_OnError_IncrementsRetryCount()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Test task", Priority.Medium, PipelineState.Planning, retryCount: 1, maxRetries: 5);

        // Act
        var result = await _sut.HandleAgentCompletionAsync(taskId, AgentCompletionResult.Error);

        // Assert
        result!.RetryCount.Should().Be(2);
        result.IsPaused.Should().BeFalse();
    }

    [Fact]
    public async Task HandleAgentCompletion_OnError_AutoPausesAfterMaxRetries()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Test task", Priority.Medium, PipelineState.Planning, retryCount: 2, maxRetries: 3);

        // Act
        var result = await _sut.HandleAgentCompletionAsync(taskId, AgentCompletionResult.Error);

        // Assert
        result!.RetryCount.Should().Be(3);
        result.IsPaused.Should().BeTrue();
        result.PauseReason.Should().Contain("Auto-paused");
    }

    [Fact]
    public async Task HandleAgentCompletion_OnError_EmitsTaskPausedEvent()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Test task", Priority.Medium, PipelineState.Planning, retryCount: 2, maxRetries: 3);

        // Act
        await _sut.HandleAgentCompletionAsync(taskId, AgentCompletionResult.Error);

        // Assert
        await _sseMock.Received(1).EmitTaskPausedAsync(Arg.Is<TaskDto>(t => t.IsPaused));
    }

    [Fact]
    public async Task HandleAgentCompletion_OnCancelled_DoesNotChangeState()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Test task", Priority.Medium, PipelineState.Planning);

        // Act
        var result = await _sut.HandleAgentCompletionAsync(taskId, AgentCompletionResult.Cancelled);

        // Assert
        result!.State.Should().Be(PipelineState.Planning);
        result.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task HandleAgentCompletion_WithNonExistentTask_ReturnsNull()
    {
        // Act
        var result = await _sut.HandleAgentCompletionAsync(Guid.NewGuid(), AgentCompletionResult.Success);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PauseTaskAsync

    [Fact]
    public async Task PauseTask_SetsIsPausedAndReason()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Test task", Priority.Medium, PipelineState.Planning);

        // Act
        var result = await _sut.PauseTaskAsync(taskId, "Manual pause for testing");

        // Assert
        result.Should().NotBeNull();
        result!.IsPaused.Should().BeTrue();
        result.PauseReason.Should().Be("Manual pause for testing");
        result.PausedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PauseTask_EmitsTaskPausedEvent()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Test task", Priority.Medium, PipelineState.Planning);

        // Act
        await _sut.PauseTaskAsync(taskId, "Test reason");

        // Assert
        await _sseMock.Received(1).EmitTaskPausedAsync(Arg.Is<TaskDto>(t => t.IsPaused));
    }

    [Fact]
    public async Task PauseTask_WithNonExistentTask_ReturnsNull()
    {
        // Act
        var result = await _sut.PauseTaskAsync(Guid.NewGuid(), "Reason");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ResumeTaskAsync

    [Fact]
    public async Task ResumeTask_ClearsPauseAndErrorState()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Paused task", Priority.Medium, PipelineState.Planning,
            isPaused: true, hasError: true, retryCount: 2);

        // Act
        var result = await _sut.ResumeTaskAsync(taskId);

        // Assert
        result.Should().NotBeNull();
        result!.IsPaused.Should().BeFalse();
        result.PauseReason.Should().BeNull();
        result.PausedAt.Should().BeNull();
        result.HasError.Should().BeFalse();
        result.RetryCount.Should().Be(0);
    }

    [Fact]
    public async Task ResumeTask_EmitsTaskResumedEvent()
    {
        // Arrange
        var taskId = await CreateTaskAsync("Paused task", Priority.Medium, PipelineState.Planning, isPaused: true);

        // Act
        await _sut.ResumeTaskAsync(taskId);

        // Assert
        await _sseMock.Received(1).EmitTaskResumedAsync(Arg.Is<TaskDto>(t => !t.IsPaused));
    }

    [Fact]
    public async Task ResumeTask_WithNonExistentTask_ReturnsNull()
    {
        // Act
        var result = await _sut.ResumeTaskAsync(Guid.NewGuid());

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetStatusAsync

    [Fact]
    public async Task GetStatus_ReturnsPendingAndPausedCounts()
    {
        // Arrange
        await CreateTaskAsync("Pending 1", Priority.Medium, PipelineState.Planning);
        await CreateTaskAsync("Pending 2", Priority.Medium, PipelineState.Implementing);
        await CreateTaskAsync("Paused", Priority.Medium, PipelineState.Planning, isPaused: true);
        await CreateTaskAsync("PrReady", Priority.Medium, PipelineState.PrReady); // Not schedulable

        var agentStatus = new AgentStatusDto(false, null, null);

        // Act
        var result = await _sut.GetStatusAsync(agentStatus);

        // Assert
        result.IsEnabled.Should().BeTrue();
        result.IsAgentRunning.Should().BeFalse();
        result.PendingTaskCount.Should().Be(2);
        result.PausedTaskCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStatus_ReflectsAgentStatus()
    {
        // Arrange
        var taskId = Guid.NewGuid();
        var agentStatus = new AgentStatusDto(true, taskId, null, DateTime.UtcNow);

        // Act
        var result = await _sut.GetStatusAsync(agentStatus);

        // Assert
        result.IsAgentRunning.Should().BeTrue();
        result.CurrentTaskId.Should().Be(taskId);
    }

    #endregion

    #region Enable/Disable

    [Fact]
    public void Enable_SetsIsEnabledTrue()
    {
        // Arrange
        _sut.Disable();

        // Act
        _sut.Enable();

        // Assert
        _sut.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_SetsIsEnabledFalse()
    {
        // Act
        _sut.Disable();

        // Assert
        _sut.IsEnabled.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private async Task<Guid> CreateTaskAsync(
        string title,
        Priority priority,
        PipelineState state,
        bool isPaused = false,
        bool hasError = false,
        int retryCount = 0,
        int maxRetries = 3,
        string? assignedAgentId = null)
    {
        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = $"Description for {title}",
            Priority = priority,
            State = state,
            IsPaused = isPaused,
            PauseReason = isPaused ? "Test pause reason" : null,
            PausedAt = isPaused ? DateTime.UtcNow : null,
            HasError = hasError,
            ErrorMessage = hasError ? "Test error" : null,
            RetryCount = retryCount,
            MaxRetries = maxRetries,
            AssignedAgentId = assignedAgentId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Tasks.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }

    #endregion
}
