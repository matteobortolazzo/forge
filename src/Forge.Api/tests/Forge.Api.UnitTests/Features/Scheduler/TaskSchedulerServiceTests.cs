using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Agent;
using Forge.Api.Features.Events;
using Forge.Api.Features.Notifications;
using Forge.Api.Features.Scheduler;
using Forge.Api.Features.Tasks;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forge.Api.UnitTests.Features.Scheduler;

public class TaskSchedulerServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IAgentRunnerService _agentRunnerMock;
    private readonly ISseService _sseMock;
    private readonly ForgeDbContext _db;
    private readonly IOptions<SchedulerOptions> _schedulerOptions;

    public TaskSchedulerServiceTests()
    {
        // Create a real in-memory database
        var dbOptions = new DbContextOptionsBuilder<ForgeDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _db = new ForgeDbContext(dbOptions);

        // Create mocks for external dependencies
        _agentRunnerMock = Substitute.For<IAgentRunnerService>();
        _sseMock = Substitute.For<ISseService>();

        // Create scheduler options
        _schedulerOptions = Options.Create(new SchedulerOptions
        {
            PollingIntervalSeconds = 5,
            DefaultMaxRetries = 3
        });

        // Create pipeline configuration
        var pipelineConfig = Options.Create(new PipelineConfiguration
        {
            MaxImplementationRetries = 3,
            MaxSimplificationIterations = 2,
            ConfidenceThreshold = 0.7m
        });

        // Build a service provider with real services
        var services = new ServiceCollection();
        services.AddSingleton(_db);
        services.AddSingleton(dbOptions);
        services.AddSingleton(_sseMock);
        services.AddSingleton<SchedulerState>();
        services.AddSingleton<NotificationService>();
        services.AddSingleton(_schedulerOptions);
        services.AddSingleton(pipelineConfig);
        services.AddSingleton<ILogger<SchedulerService>>(Substitute.For<ILogger<SchedulerService>>());
        services.AddSingleton<SchedulerService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose()
    {
        _db.Dispose();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task TryScheduleNextTask_WhenAgentRunning_DoesNotSchedule()
    {
        // Arrange
        _agentRunnerMock.GetStatus().Returns(new AgentStatusDto(true, Guid.NewGuid(), DateTime.UtcNow));
        await CreateTaskAsync(PipelineState.Planning, Priority.High);

        var sut = CreateService();

        // Act
        await sut.TryScheduleNextTaskAsync();

        // Assert - agent was not started
        await _agentRunnerMock.DidNotReceive().StartAgentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TryScheduleNextTask_WhenAgentIdleAndTaskAvailable_StartsAgent()
    {
        // Arrange
        _agentRunnerMock.GetStatus().Returns(new AgentStatusDto(false, null, null));
        _agentRunnerMock.StartAgentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        var taskId = await CreateTaskAsync(PipelineState.Planning, Priority.High);

        var sut = CreateService();

        // Act
        await sut.TryScheduleNextTaskAsync();

        // Assert - agent was started with correct task
        await _agentRunnerMock.Received(1).StartAgentAsync(taskId, Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TryScheduleNextTask_WhenTaskAvailable_EmitsSseEvent()
    {
        // Arrange
        _agentRunnerMock.GetStatus().Returns(new AgentStatusDto(false, null, null));
        _agentRunnerMock.StartAgentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        await CreateTaskAsync(PipelineState.Planning, Priority.High);

        var sut = CreateService();

        // Act
        await sut.TryScheduleNextTaskAsync();

        // Assert - SSE event was emitted
        await _sseMock.Received(1).EmitSchedulerTaskScheduledAsync(Arg.Any<TaskDto>());
    }

    [Fact]
    public async Task TryScheduleNextTask_WhenNoTaskAvailable_DoesNotStartAgent()
    {
        // Arrange
        _agentRunnerMock.GetStatus().Returns(new AgentStatusDto(false, null, null));
        // No tasks in the database

        var sut = CreateService();

        // Act
        await sut.TryScheduleNextTaskAsync();

        // Assert - agent was not started
        await _agentRunnerMock.DidNotReceive().StartAgentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TryScheduleNextTask_WhenSchedulerDisabled_DoesNotStartAgent()
    {
        // Arrange
        _agentRunnerMock.GetStatus().Returns(new AgentStatusDto(false, null, null));
        await CreateTaskAsync(PipelineState.Planning, Priority.High);

        var schedulerState = _serviceProvider.GetRequiredService<SchedulerState>();
        schedulerState.Disable();

        var sut = CreateService();

        // Act
        await sut.TryScheduleNextTaskAsync();

        // Assert - agent was not started because scheduler is disabled
        await _agentRunnerMock.DidNotReceive().StartAgentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task TryScheduleNextTask_SelectsHighestPriorityTask()
    {
        // Arrange
        _agentRunnerMock.GetStatus().Returns(new AgentStatusDto(false, null, null));
        _agentRunnerMock.StartAgentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await CreateTaskAsync(PipelineState.Planning, Priority.Low, "Low");
        var highPriorityId = await CreateTaskAsync(PipelineState.Planning, Priority.High, "High");
        await CreateTaskAsync(PipelineState.Planning, Priority.Medium, "Medium");

        var sut = CreateService();

        // Act
        await sut.TryScheduleNextTaskAsync();

        // Assert - high priority task was selected
        await _agentRunnerMock.Received(1).StartAgentAsync(highPriorityId, "High", Arg.Any<string>());
    }

    private TaskSchedulerService CreateService()
    {
        // Create a scope factory that returns our service provider
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        scope.ServiceProvider.Returns(_serviceProvider);
        scopeFactory.CreateScope().Returns(scope);

        return new TaskSchedulerService(
            scopeFactory,
            _agentRunnerMock,
            _sseMock,
            _schedulerOptions,
            Substitute.For<ILogger<TaskSchedulerService>>());
    }

    private async Task<Guid> CreateTaskAsync(PipelineState state, Priority priority, string title = "Test Task")
    {
        var entity = new TaskEntity
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = "Test description",
            State = state,
            Priority = priority,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Tasks.Add(entity);
        await _db.SaveChangesAsync();
        return entity.Id;
    }
}
