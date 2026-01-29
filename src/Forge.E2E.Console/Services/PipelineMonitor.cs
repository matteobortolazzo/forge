using System.Text.Json;
using Forge.E2E.Console.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forge.E2E.Console.Services;

/// <summary>
/// Exception thrown when an agent operation times out.
/// </summary>
public sealed class AgentTimeoutException : Exception
{
    public AgentTimeoutException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when the pipeline encounters an error.
/// </summary>
public sealed class PipelineException : Exception
{
    public PipelineException(string message) : base(message) { }
    public PipelineException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>
/// Orchestrates the E2E test flow through the complete pipeline.
/// </summary>
public sealed class PipelineMonitor
{
    private readonly ForgeApiClient _api;
    private readonly SseEventListener _sse;
    private readonly ILogger<PipelineMonitor> _logger;
    private readonly TimeoutOptions _timeouts;
    private readonly E2EOptions _e2eOptions;
    private readonly TestRepositoryOptions _repoOptions;

    private CancellationTokenSource? _sseCts;
    private Task? _sseTask;
    private readonly List<SseEvent> _pendingEvents = [];
    private readonly object _eventLock = new();

    public PipelineMonitor(
        ForgeApiClient api,
        SseEventListener sse,
        IOptions<TimeoutOptions> timeouts,
        IOptions<E2EOptions> e2eOptions,
        IOptions<TestRepositoryOptions> repoOptions,
        ILogger<PipelineMonitor> logger)
    {
        _api = api;
        _sse = sse;
        _logger = logger;
        _timeouts = timeouts.Value;
        _e2eOptions = e2eOptions.Value;
        _repoOptions = repoOptions.Value;
    }

    /// <summary>
    /// Runs the complete E2E test.
    /// </summary>
    public async Task<bool> RunAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("=== Forge E2E Pipeline Test ===");

        try
        {
            // Phase 1: Setup
            _logger.LogInformation("Phase 1: Setup");
            await _api.DisableSchedulerAsync(ct);
            await AbortAnyRunningAgentAsync(ct);
            var repository = await CreateOrGetRepositoryAsync(ct);
            StartSseListener(ct);

            // Phase 2: Create and refine backlog item
            _logger.LogInformation("Phase 2: Backlog Item Creation & Refinement");
            var backlogItem = await CreateBacklogItemAsync(repository.Id, ct);
            backlogItem = await RunRefiningPhaseAsync(repository.Id, backlogItem.Id, ct);

            // Phase 3: Splitting
            _logger.LogInformation("Phase 3: Splitting");
            backlogItem = await RunSplittingPhaseAsync(repository.Id, backlogItem.Id, ct);

            // Phase 4: Task Execution
            _logger.LogInformation("Phase 4: Task Execution");
            await RunTaskExecutionPhaseAsync(repository.Id, backlogItem.Id, ct);

            // Phase 5: Verification
            _logger.LogInformation("Phase 5: Verification");
            await VerifyCompletionAsync(repository.Id, backlogItem.Id, ct);

            _logger.LogInformation("=== E2E Test Completed Successfully ===");
            return true;
        }
        catch (AgentTimeoutException ex)
        {
            _logger.LogError(ex, "Agent timeout: {Message}", ex.Message);
            return false;
        }
        catch (PipelineException ex)
        {
            _logger.LogError(ex, "Pipeline error: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error: {Message}", ex.Message);
            return false;
        }
        finally
        {
            StopSseListener();
        }
    }

    #region Phase 1: Setup

    private async Task AbortAnyRunningAgentAsync(CancellationToken ct)
    {
        var status = await _api.GetAgentStatusAsync(ct);
        if (!status.IsRunning)
        {
            return;
        }

        _logger.LogInformation("Aborting running agent from previous session...");

        // Get the repository for the running item
        if (status.CurrentBacklogItemId.HasValue)
        {
            var repos = await _api.GetRepositoriesAsync(ct);
            foreach (var repo in repos)
            {
                try
                {
                    await _api.AbortBacklogAgentAsync(repo.Id, status.CurrentBacklogItemId.Value, ct);
                    _logger.LogInformation("Aborted backlog item agent");
                    return;
                }
                catch
                {
                    // Not in this repository, try next
                }
            }
        }

        if (status.CurrentTaskId.HasValue)
        {
            var repos = await _api.GetRepositoriesAsync(ct);
            foreach (var repo in repos)
            {
                try
                {
                    await _api.AbortTaskAgentAsync(repo.Id, status.CurrentTaskId.Value, ct);
                    _logger.LogInformation("Aborted task agent");
                    return;
                }
                catch
                {
                    // Not in this repository, try next
                }
            }
        }

        // Wait a moment for the agent to actually stop
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
    }

    private async Task<RepositoryDto> CreateOrGetRepositoryAsync(CancellationToken ct)
    {
        var repos = await _api.GetRepositoriesAsync(ct);
        var existing = repos.FirstOrDefault(r => r.Path == _repoOptions.Path);

        if (existing != null)
        {
            _logger.LogInformation("Using existing repository: {Name} ({Id})", existing.Name, existing.Id);
            return existing;
        }

        if (_repoOptions.CreateIfMissing)
        {
            // Ensure the directory exists
            if (!Directory.Exists(_repoOptions.Path))
            {
                _logger.LogInformation("Creating test repository directory: {Path}", _repoOptions.Path);
                Directory.CreateDirectory(_repoOptions.Path);

                // Initialize git repo
                await InitializeGitRepoAsync(_repoOptions.Path, ct);
            }

            var dto = new CreateRepositoryDto("E2E Test Repo", _repoOptions.Path, IsDefault: true);
            var repo = await _api.CreateRepositoryAsync(dto, ct);
            _logger.LogInformation("Created repository: {Name} ({Id})", repo.Name, repo.Id);
            return repo;
        }

        throw new PipelineException($"Repository at {_repoOptions.Path} does not exist and CreateIfMissing is false");
    }

    private static async Task InitializeGitRepoAsync(string path, CancellationToken ct)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "git",
            Arguments = "init",
            WorkingDirectory = path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = System.Diagnostics.Process.Start(psi);
        if (process != null)
        {
            await process.WaitForExitAsync(ct);
        }
    }

    private void StartSseListener(CancellationToken ct)
    {
        _sseCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        _sseTask = Task.Run(async () =>
        {
            try
            {
                await foreach (var evt in _sse.GetEventsAsync(_sseCts.Token))
                {
                    lock (_eventLock)
                    {
                        _pendingEvents.Add(evt);
                    }

                    _logger.LogDebug("SSE Event: {Type}", evt.Type);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SSE listener error");
            }
        }, _sseCts.Token);
    }

    private void StopSseListener()
    {
        _sseCts?.Cancel();
        _sseCts?.Dispose();
        _sseCts = null;
    }

    #endregion

    #region Phase 2: Backlog Item Creation & Refinement

    private async Task<BacklogItemDto> CreateBacklogItemAsync(Guid repoId, CancellationToken ct)
    {
        var dto = new CreateBacklogItemDto(
            Title: "Add greeting utility",
            Description: "Create a utility function that formats greetings",
            Priority: Priority.Medium,
            AcceptanceCriteria: "- Function accepts name parameter\n- Returns formatted greeting");

        var backlogItem = await _api.CreateBacklogItemAsync(repoId, dto, ct);
        _logger.LogInformation("Created backlog item: {Title} ({Id})", backlogItem.Title, backlogItem.Id);
        return backlogItem;
    }

    private async Task<BacklogItemDto> RunRefiningPhaseAsync(Guid repoId, Guid backlogItemId, CancellationToken ct)
    {
        // Transition to Refining if not already
        var backlogItem = await _api.GetBacklogItemAsync(repoId, backlogItemId, ct);
        if (backlogItem.State == BacklogItemState.New)
        {
            backlogItem = await _api.TransitionBacklogItemAsync(repoId, backlogItemId,
                new TransitionBacklogItemDto(BacklogItemState.Refining), ct);
        }

        // Start the refining agent
        backlogItem = await _api.StartBacklogAgentAsync(repoId, backlogItemId, ct);
        _logger.LogInformation("Started refining agent for backlog item {Id}", backlogItemId);

        // Wait for state to change to Ready
        var timeout = TimeSpan.FromMinutes(_timeouts.RefiningMinutes);
        backlogItem = await WaitForBacklogStateAsync(repoId, backlogItemId, BacklogItemState.Ready, timeout, ct);

        _logger.LogInformation("Backlog item refined and ready: {Title}", backlogItem.Title);
        return backlogItem;
    }

    #endregion

    #region Phase 3: Splitting

    private async Task<BacklogItemDto> RunSplittingPhaseAsync(Guid repoId, Guid backlogItemId, CancellationToken ct)
    {
        // Transition to Splitting
        var backlogItem = await _api.TransitionBacklogItemAsync(repoId, backlogItemId,
            new TransitionBacklogItemDto(BacklogItemState.Splitting), ct);

        // Start the splitting agent
        backlogItem = await _api.StartBacklogAgentAsync(repoId, backlogItemId, ct);
        _logger.LogInformation("Started splitting agent for backlog item {Id}", backlogItemId);

        // Wait for state to change to Executing (meaning tasks were created)
        var timeout = TimeSpan.FromMinutes(_timeouts.SplittingMinutes);
        backlogItem = await WaitForBacklogStateAsync(repoId, backlogItemId, BacklogItemState.Executing, timeout, ct);

        _logger.LogInformation("Backlog item split into {Count} tasks", backlogItem.TaskCount);
        return backlogItem;
    }

    #endregion

    #region Phase 4: Task Execution

    private async Task RunTaskExecutionPhaseAsync(Guid repoId, Guid backlogItemId, CancellationToken ct)
    {
        var tasks = await _api.GetTasksAsync(repoId, backlogItemId, ct);
        var orderedTasks = tasks.OrderBy(t => t.ExecutionOrder).ToList();

        _logger.LogInformation("Executing {Count} tasks in order", orderedTasks.Count);

        foreach (var task in orderedTasks)
        {
            await ExecuteTaskAsync(repoId, task.Id, ct);
        }
    }

    private async Task ExecuteTaskAsync(Guid repoId, Guid taskId, CancellationToken ct)
    {
        var task = await _api.GetTaskAsync(repoId, taskId, ct);
        _logger.LogInformation("Executing task {Order}: {Title} ({Id})", task.ExecutionOrder, task.Title, task.Id);

        // Planning phase
        if (task.State == PipelineState.Planning)
        {
            task = await _api.StartTaskAgentAsync(repoId, taskId, ct);
            _logger.LogInformation("  Started planning agent");

            var timeout = TimeSpan.FromMinutes(_timeouts.PlanningMinutes);
            task = await WaitForTaskStateAsync(repoId, taskId, PipelineState.Implementing, timeout, ct);
            _logger.LogInformation("  Planning complete");
        }

        // Implementing phase
        if (task.State == PipelineState.Implementing)
        {
            task = await _api.StartTaskAgentAsync(repoId, taskId, ct);
            _logger.LogInformation("  Started implementing agent");

            var timeout = TimeSpan.FromMinutes(_timeouts.ImplementingMinutes);
            task = await WaitForTaskStateAsync(repoId, taskId, PipelineState.PrReady, timeout, ct);
            _logger.LogInformation("  Implementation complete");
        }

        _logger.LogInformation("Task completed: {Title}", task.Title);
    }

    #endregion

    #region Phase 5: Verification

    private async Task VerifyCompletionAsync(Guid repoId, Guid backlogItemId, CancellationToken ct)
    {
        var backlogItem = await _api.GetBacklogItemAsync(repoId, backlogItemId, ct);

        if (backlogItem.State != BacklogItemState.Done)
        {
            throw new PipelineException($"Expected backlog item state Done, got {backlogItem.State}");
        }

        var tasks = await _api.GetTasksAsync(repoId, backlogItemId, ct);
        var incompleteTasks = tasks.Where(t => t.State != PipelineState.PrReady).ToList();

        if (incompleteTasks.Count != 0)
        {
            var incomplete = string.Join(", ", incompleteTasks.Select(t => $"{t.Title} ({t.State})"));
            throw new PipelineException($"Not all tasks reached PrReady: {incomplete}");
        }

        // Collect and report artifacts
        var artifacts = await _api.GetBacklogArtifactsAsync(repoId, backlogItemId, ct);
        _logger.LogInformation("Backlog artifacts: {Count}", artifacts.Count);
        foreach (var artifact in artifacts)
        {
            _logger.LogInformation("  - {Type}: {Id}", artifact.ArtifactType, artifact.Id);
        }

        foreach (var task in tasks)
        {
            var taskArtifacts = await _api.GetTaskArtifactsAsync(repoId, task.Id, ct);
            _logger.LogInformation("Task '{Title}' artifacts: {Count}", task.Title, taskArtifacts.Count);
            foreach (var artifact in taskArtifacts)
            {
                _logger.LogInformation("  - {Type}: {Id}", artifact.ArtifactType, artifact.Id);
            }
        }

        _logger.LogInformation("Verification passed: Backlog item {Title} is Done with {Count} tasks all at PrReady",
            backlogItem.Title, tasks.Count);
    }

    #endregion

    #region State Waiting Helpers

    private async Task<BacklogItemDto> WaitForBacklogStateAsync(
        Guid repoId,
        Guid backlogItemId,
        BacklogItemState targetState,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        while (!timeoutCts.Token.IsCancellationRequested)
        {
            // Check pending gates and questions
            await HandlePendingGatesAsync(ct);
            await HandlePendingQuestionsAsync(ct);

            // Process SSE events
            var events = DrainEvents();
            foreach (var evt in events)
            {
                if (evt.Type == SseEventTypes.BacklogItemUpdated)
                {
                    var dto = evt.Payload.Deserialize<BacklogItemDto>(ForgeApiClient.JsonOptions);
                    if (dto?.Id == backlogItemId)
                    {
                        _logger.LogDebug("Backlog item state: {State}", dto.State);
                        if (dto.State == targetState)
                        {
                            return dto;
                        }
                        if (dto.HasError)
                        {
                            throw new PipelineException($"Backlog item error: {dto.ErrorMessage}");
                        }
                    }
                }
            }

            // Poll as fallback
            var backlogItem = await _api.GetBacklogItemAsync(repoId, backlogItemId, ct);
            if (backlogItem.State == targetState)
            {
                return backlogItem;
            }
            if (backlogItem.HasError)
            {
                throw new PipelineException($"Backlog item error: {backlogItem.ErrorMessage}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), timeoutCts.Token);
        }

        throw new AgentTimeoutException($"Timeout waiting for backlog item to reach state {targetState}");
    }

    private async Task<TaskDto> WaitForTaskStateAsync(
        Guid repoId,
        Guid taskId,
        PipelineState targetState,
        TimeSpan timeout,
        CancellationToken ct)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        while (!timeoutCts.Token.IsCancellationRequested)
        {
            // Check pending gates and questions
            await HandlePendingGatesAsync(ct);
            await HandlePendingQuestionsAsync(ct);

            // Process SSE events
            var events = DrainEvents();
            foreach (var evt in events)
            {
                if (evt.Type == SseEventTypes.TaskUpdated)
                {
                    var dto = evt.Payload.Deserialize<TaskDto>(ForgeApiClient.JsonOptions);
                    if (dto?.Id == taskId)
                    {
                        _logger.LogDebug("Task state: {State}", dto.State);
                        if (dto.State == targetState)
                        {
                            return dto;
                        }
                        if (dto.HasError)
                        {
                            throw new PipelineException($"Task error: {dto.ErrorMessage}");
                        }
                    }
                }
            }

            // Poll as fallback
            var task = await _api.GetTaskAsync(repoId, taskId, ct);
            if (task.State == targetState)
            {
                return task;
            }
            if (task.HasError)
            {
                throw new PipelineException($"Task error: {task.ErrorMessage}");
            }

            await Task.Delay(TimeSpan.FromSeconds(2), timeoutCts.Token);
        }

        throw new AgentTimeoutException($"Timeout waiting for task to reach state {targetState}");
    }

    private List<SseEvent> DrainEvents()
    {
        lock (_eventLock)
        {
            var events = _pendingEvents.ToList();
            _pendingEvents.Clear();
            return events;
        }
    }

    #endregion

    #region Gate and Question Handling

    private async Task HandlePendingGatesAsync(CancellationToken ct)
    {
        if (!_e2eOptions.AutoApproveGates)
            return;

        var gates = await _api.GetPendingGatesAsync(ct);
        foreach (var gate in gates)
        {
            _logger.LogInformation("Auto-approving gate: {Type} ({Id})", gate.GateType, gate.Id);
            await _api.ResolveGateAsync(gate.Id,
                new ResolveHumanGateDto(HumanGateStatus.Approved, "Auto-approved by E2E test", "E2E Console"),
                ct);
        }
    }

    private async Task HandlePendingQuestionsAsync(CancellationToken ct)
    {
        if (!_e2eOptions.AutoAnswerQuestions)
            return;

        var question = await _api.GetPendingQuestionAsync(ct);
        if (question != null)
        {
            _logger.LogInformation("Auto-answering question: {Id}", question.Id);

            // Build answers - select first option for each question
            var answers = question.Questions.Select((q, i) => new QuestionAnswer(
                QuestionIndex: i,
                SelectedOptionIndices: [0], // Select first option
                CustomAnswer: null
            )).ToList();

            await _api.AnswerQuestionAsync(question.Id, new SubmitAnswerDto(answers), ct);
        }
    }

    #endregion
}
