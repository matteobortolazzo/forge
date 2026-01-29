using System.Text;
using System.Text.Json;
using Claude.CodeSdk;
using Claude.CodeSdk.ContentBlocks;
using Claude.CodeSdk.Messages;
using Claude.CodeSdk.Permissions;
using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Agents;
using Forge.Api.Features.AgentQuestions;
using Forge.Api.Features.Backlog;
using Forge.Api.Features.Events;
using Forge.Api.Features.Scheduler;
using Forge.Api.Features.Tasks;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Forge.Api.Features.Agent;

public class AgentRunnerService(
    IServiceScopeFactory scopeFactory,
    ISseService sse,
    IConfiguration configuration,
    IClaudeAgentClientFactory clientFactory,
    IOrchestratorService orchestrator,
    IArtifactParser artifactParser,
    AgentQuestionWaiter questionWaiter,
    IOptions<AgentQuestionsOptions> questionOptions,
    ILogger<AgentRunnerService> logger) : IAgentRunnerService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Lock _lock = new();
    private CancellationTokenSource? _cts;
    private Guid? _currentTaskId;
    private Guid? _currentBacklogItemId;
    private DateTime? _startedAt;

    public AgentStatusDto GetStatus()
    {
        lock (_lock)
        {
            return new AgentStatusDto(
                _currentTaskId.HasValue || _currentBacklogItemId.HasValue,
                _currentTaskId,
                _currentBacklogItemId,
                _startedAt);
        }
    }

    public async Task<bool> StartAgentAsync(Guid taskId, string title, string description)
    {
        lock (_lock)
        {
            if (_currentTaskId.HasValue || _currentBacklogItemId.HasValue)
            {
                logger.LogWarning("Agent already running");
                return false;
            }

            _currentTaskId = taskId;
            _startedAt = DateTime.UtcNow;
            _cts = new CancellationTokenSource();
        }

        await sse.EmitAgentStatusChangedAsync(GetStatus());

        // Update task to assign agent
        using (var scope = scopeFactory.CreateScope())
        {
            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
            await taskService.SetAgentAsync(taskId, "claude-agent");
        }

        // Fire and forget the agent execution
        _ = RunTaskAgentAsync(taskId, title, description, _cts.Token);

        return true;
    }

    public async Task<bool> StartBacklogAgentAsync(Guid backlogItemId, string title, string description)
    {
        lock (_lock)
        {
            if (_currentTaskId.HasValue || _currentBacklogItemId.HasValue)
            {
                logger.LogWarning("Agent already running");
                return false;
            }

            _currentBacklogItemId = backlogItemId;
            _startedAt = DateTime.UtcNow;
            _cts = new CancellationTokenSource();
        }

        await sse.EmitAgentStatusChangedAsync(GetStatus());

        // Update backlog item to assign agent
        using (var scope = scopeFactory.CreateScope())
        {
            var backlogService = scope.ServiceProvider.GetRequiredService<BacklogService>();
            await backlogService.SetAgentAsync(backlogItemId, "claude-agent");
        }

        // Fire and forget the agent execution
        _ = RunBacklogAgentAsync(backlogItemId, title, description, _cts.Token);

        return true;
    }

    public async Task<bool> AbortAsync()
    {
        CancellationTokenSource? cts;
        Guid? taskIdToCancel = null;
        Guid? backlogItemIdToCancel = null;

        lock (_lock)
        {
            if (_cts is null)
            {
                return false;
            }

            cts = _cts;
            taskIdToCancel = _currentTaskId;
            backlogItemIdToCancel = _currentBacklogItemId;
        }

        logger.LogInformation("Aborting agent");

        // Cancel any pending questions
        using (var scope = scopeFactory.CreateScope())
        {
            var questionService = scope.ServiceProvider.GetRequiredService<AgentQuestionService>();

            if (taskIdToCancel.HasValue)
            {
                await questionService.CancelAllForTaskAsync(taskIdToCancel.Value, questionWaiter);
            }
            else if (backlogItemIdToCancel.HasValue)
            {
                await questionService.CancelAllForBacklogItemAsync(backlogItemIdToCancel.Value, questionWaiter);
            }
        }

        // Cancel the token
        await cts.CancelAsync();

        return true;
    }

    private async Task RunTaskAgentAsync(Guid taskId, string title, string description, CancellationToken ct)
    {
        var completionResult = AgentCompletionResult.Success;
        var agentOutput = new StringBuilder();
        ResolvedAgentConfig? resolvedConfig = null;
        PipelineState? taskState = null;

        try
        {
            var cliPath = configuration["CLAUDE_CODE_PATH"];

            // Load the task entity with repository for orchestration
            TaskEntity? task;
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();
                task = await db.Tasks
                    .Include(t => t.Repository)
                    .FirstOrDefaultAsync(t => t.Id == taskId, ct);
            }

            if (task == null)
            {
                logger.LogError("Task {TaskId} not found", taskId);
                completionResult = AgentCompletionResult.Error;
                return;
            }

            // Get working directory from task's repository
            var workingDirectory = task.Repository.Path;

            taskState = task.State;

            // Use orchestrator to select agent and build prompt
            resolvedConfig = await orchestrator.SelectAgentForTaskAsync(task, workingDirectory);

            logger.LogInformation(
                "Using agent {AgentId} for task {TaskId} in state {State}",
                resolvedConfig.Config.Id, taskId, task.State);

            var options = new ClaudeAgentOptions
            {
                WorkingDirectory = workingDirectory,
                CliPath = cliPath,
                DangerouslySkipPermissions = true,
                ToolPermissionHandler = CreateToolPermissionHandler(taskId, null, ct),
                ToolPermissionTimeoutMs = questionOptions.Value.TimeoutSeconds * 1000,
                MaxTurns = resolvedConfig.MaxTurns
            };

            await using var client = clientFactory.Create(options);

            await foreach (var message in client.QueryStreamAsync(resolvedConfig.ResolvedPrompt, ct: ct))
            {
                // Capture text output for artifact parsing
                if (message is AssistantMessage assistant)
                {
                    foreach (var block in assistant.Content)
                    {
                        if (block is TextBlock text && !string.IsNullOrWhiteSpace(text.Text))
                        {
                            agentOutput.AppendLine(text.Text);
                        }
                    }
                }

                await ProcessTaskMessageAsync(taskId, message);
            }

            // Agent completed successfully
            logger.LogInformation("Agent completed task {TaskId} successfully", taskId);
            completionResult = AgentCompletionResult.Success;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Agent was cancelled for task {TaskId}", taskId);
            await AddTaskLogAsync(taskId, LogType.Info, "Agent execution was cancelled.");
            completionResult = AgentCompletionResult.Cancelled;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent failed for task {TaskId}", taskId);
            await AddTaskLogAsync(taskId, LogType.Error, $"Agent error: {ex.Message}");

            // Set error on task
            using var scope = scopeFactory.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
            await taskService.SetErrorAsync(taskId, ex.Message);
            completionResult = AgentCompletionResult.Error;
        }
        finally
        {
            // Store artifact if we have output and config
            if (completionResult == AgentCompletionResult.Success &&
                resolvedConfig != null &&
                agentOutput.Length > 0 &&
                taskState.HasValue)
            {
                await StoreTaskArtifactAsync(taskId, taskState.Value, resolvedConfig, agentOutput.ToString());
            }

            await CleanupTaskAsync(taskId, completionResult);
        }
    }

    private async Task RunBacklogAgentAsync(Guid backlogItemId, string title, string description, CancellationToken ct)
    {
        var completionResult = AgentCompletionResult.Success;
        var agentOutput = new StringBuilder();
        ResolvedAgentConfig? resolvedConfig = null;
        BacklogItemState? backlogState = null;

        try
        {
            var cliPath = configuration["CLAUDE_CODE_PATH"];

            // Load the backlog item entity with repository for orchestration
            BacklogItemEntity? backlogItem;
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();
                backlogItem = await db.BacklogItems
                    .Include(b => b.Repository)
                    .FirstOrDefaultAsync(b => b.Id == backlogItemId, ct);
            }

            if (backlogItem == null)
            {
                logger.LogError("BacklogItem {BacklogItemId} not found", backlogItemId);
                completionResult = AgentCompletionResult.Error;
                return;
            }

            // Get working directory from backlog item's repository
            var workingDirectory = backlogItem.Repository.Path;

            backlogState = backlogItem.State;

            // Use orchestrator to select agent and build prompt
            resolvedConfig = await orchestrator.SelectAgentForBacklogItemAsync(backlogItem, workingDirectory);

            logger.LogInformation(
                "Using agent {AgentId} for backlog item {BacklogItemId} in state {State}",
                resolvedConfig.Config.Id, backlogItemId, backlogItem.State);

            var options = new ClaudeAgentOptions
            {
                WorkingDirectory = workingDirectory,
                CliPath = cliPath,
                DangerouslySkipPermissions = true,
                ToolPermissionHandler = CreateToolPermissionHandler(null, backlogItemId, ct),
                ToolPermissionTimeoutMs = questionOptions.Value.TimeoutSeconds * 1000,
                MaxTurns = resolvedConfig.MaxTurns
            };

            await using var client = clientFactory.Create(options);

            await foreach (var message in client.QueryStreamAsync(resolvedConfig.ResolvedPrompt, ct: ct))
            {
                // Capture text output for artifact parsing
                if (message is AssistantMessage assistant)
                {
                    foreach (var block in assistant.Content)
                    {
                        if (block is TextBlock text && !string.IsNullOrWhiteSpace(text.Text))
                        {
                            agentOutput.AppendLine(text.Text);
                        }
                    }
                }

                await ProcessBacklogMessageAsync(backlogItemId, message);
            }

            // Agent completed successfully
            logger.LogInformation("Agent completed backlog item {BacklogItemId} successfully", backlogItemId);
            completionResult = AgentCompletionResult.Success;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Agent was cancelled for backlog item {BacklogItemId}", backlogItemId);
            await AddBacklogLogAsync(backlogItemId, LogType.Info, "Agent execution was cancelled.");
            completionResult = AgentCompletionResult.Cancelled;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent failed for backlog item {BacklogItemId}", backlogItemId);
            await AddBacklogLogAsync(backlogItemId, LogType.Error, $"Agent error: {ex.Message}");

            // Set error on backlog item
            using var scope = scopeFactory.CreateScope();
            var backlogService = scope.ServiceProvider.GetRequiredService<BacklogService>();
            await backlogService.SetErrorAsync(backlogItemId, ex.Message);
            completionResult = AgentCompletionResult.Error;
        }
        finally
        {
            // Store artifact if we have output and config
            if (completionResult == AgentCompletionResult.Success &&
                resolvedConfig != null &&
                agentOutput.Length > 0 &&
                backlogState.HasValue)
            {
                await StoreBacklogArtifactAsync(backlogItemId, backlogState.Value, resolvedConfig, agentOutput.ToString());
            }

            await CleanupBacklogAsync(backlogItemId, completionResult);
        }
    }

    private async Task StoreTaskArtifactAsync(
        Guid taskId,
        PipelineState taskState,
        ResolvedAgentConfig resolvedConfig,
        string output)
    {
        try
        {
            // Parse artifact from output
            var parsed = artifactParser.ParseTaskArtifact(output, resolvedConfig.Config);
            if (parsed != null)
            {
                // Store the artifact
                await orchestrator.StoreTaskArtifactAsync(
                    taskId,
                    taskState,
                    parsed.Type,
                    parsed.Content,
                    resolvedConfig.Config.Id);

                // Parse and store recommended next state
                var recommendedState = artifactParser.ParseRecommendedNextState(output);
                if (recommendedState.HasValue)
                {
                    await orchestrator.UpdateTaskContextAsync(taskId, null, null, recommendedState);
                    logger.LogInformation(
                        "Agent recommended next state {State} for task {TaskId}",
                        recommendedState, taskId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store artifact for task {TaskId}", taskId);
        }
    }

    private async Task StoreBacklogArtifactAsync(
        Guid backlogItemId,
        BacklogItemState backlogState,
        ResolvedAgentConfig resolvedConfig,
        string output)
    {
        try
        {
            // Parse artifact from output
            var parsed = artifactParser.ParseBacklogArtifact(output, resolvedConfig.Config);
            if (parsed != null)
            {
                // Store the artifact
                await orchestrator.StoreBacklogArtifactAsync(
                    backlogItemId,
                    backlogState,
                    parsed.Type,
                    parsed.Content,
                    resolvedConfig.Config.Id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store artifact for backlog item {BacklogItemId}", backlogItemId);
        }
    }

    private async Task ProcessTaskMessageAsync(Guid taskId, IMessage message)
    {
        switch (message)
        {
            case AssistantMessage assistant:
                foreach (var block in assistant.Content)
                {
                    switch (block)
                    {
                        case TextBlock text when !string.IsNullOrWhiteSpace(text.Text):
                            // Check if it looks like thinking/reasoning
                            var logType = text.Text.StartsWith("I ") || text.Text.Contains("Let me")
                                ? LogType.Thinking
                                : LogType.Info;
                            await AddTaskLogAsync(taskId, logType, text.Text);
                            break;

                        case ToolUseBlock tool:
                            var toolInput = tool.Input.ToString();
                            var content = $"Using {tool.Name}: {TruncateContent(toolInput, 500)}";
                            await AddTaskLogAsync(taskId, LogType.ToolUse, content, tool.Name);
                            break;

                        case ToolResultBlock result:
                            var resultContent = TruncateContent(result.Content, 500);
                            var resultType = result.IsError ? LogType.Error : LogType.ToolResult;
                            await AddTaskLogAsync(taskId, resultType, resultContent);
                            break;
                    }
                }
                break;

            case ResultMessage result:
                var summary = $"Completed in {result.NumTurns} turns. " +
                              $"Tokens: {result.Usage.TotalTokens} (in: {result.Usage.InputTokens}, out: {result.Usage.OutputTokens})";
                if (result.CostUsd.HasValue)
                {
                    summary += $", Cost: ${result.CostUsd:F4}";
                }
                await AddTaskLogAsync(taskId, LogType.Info, summary);
                break;
        }
    }

    private async Task ProcessBacklogMessageAsync(Guid backlogItemId, IMessage message)
    {
        switch (message)
        {
            case AssistantMessage assistant:
                foreach (var block in assistant.Content)
                {
                    switch (block)
                    {
                        case TextBlock text when !string.IsNullOrWhiteSpace(text.Text):
                            var logType = text.Text.StartsWith("I ") || text.Text.Contains("Let me")
                                ? LogType.Thinking
                                : LogType.Info;
                            await AddBacklogLogAsync(backlogItemId, logType, text.Text);
                            break;

                        case ToolUseBlock tool:
                            var toolInput = tool.Input.ToString();
                            var content = $"Using {tool.Name}: {TruncateContent(toolInput, 500)}";
                            await AddBacklogLogAsync(backlogItemId, LogType.ToolUse, content, tool.Name);
                            break;

                        case ToolResultBlock result:
                            var resultContent = TruncateContent(result.Content, 500);
                            var resultType = result.IsError ? LogType.Error : LogType.ToolResult;
                            await AddBacklogLogAsync(backlogItemId, resultType, resultContent);
                            break;
                    }
                }
                break;

            case ResultMessage result:
                var summary = $"Completed in {result.NumTurns} turns. " +
                              $"Tokens: {result.Usage.TotalTokens} (in: {result.Usage.InputTokens}, out: {result.Usage.OutputTokens})";
                if (result.CostUsd.HasValue)
                {
                    summary += $", Cost: ${result.CostUsd:F4}";
                }
                await AddBacklogLogAsync(backlogItemId, LogType.Info, summary);
                break;
        }
    }

    private async Task AddTaskLogAsync(Guid taskId, LogType type, string content, string? toolName = null)
    {
        using var scope = scopeFactory.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
        await taskService.AddLogAsync(taskId, type, content, toolName);
    }

    private async Task AddBacklogLogAsync(Guid backlogItemId, LogType type, string content, string? toolName = null)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ForgeDbContext>();

        var entity = new TaskLogEntity
        {
            Id = Guid.NewGuid(),
            BacklogItemId = backlogItemId,
            Type = type,
            Content = content,
            ToolName = toolName,
            Timestamp = DateTime.UtcNow
        };

        db.TaskLogs.Add(entity);
        await db.SaveChangesAsync();

        var logDto = new BacklogItemLogDto(
            entity.Id,
            backlogItemId,
            entity.Type,
            entity.Content,
            entity.ToolName,
            entity.Timestamp);

        await sse.EmitBacklogItemLogAsync(logDto);
    }

    private async Task CleanupTaskAsync(Guid taskId, AgentCompletionResult completionResult)
    {
        lock (_lock)
        {
            _cts?.Dispose();
            _cts = null;
            _currentTaskId = null;
            _startedAt = null;
        }

        // Clear agent assignment and handle completion via scheduler
        using (var scope = scopeFactory.CreateScope())
        {
            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
            await taskService.SetAgentAsync(taskId, null);

            // Let scheduler handle auto-transition based on completion result
            var schedulerService = scope.ServiceProvider.GetRequiredService<SchedulerService>();
            await schedulerService.HandleAgentCompletionAsync(taskId, completionResult);
        }

        await sse.EmitAgentStatusChangedAsync(GetStatus());
    }

    private async Task CleanupBacklogAsync(Guid backlogItemId, AgentCompletionResult completionResult)
    {
        lock (_lock)
        {
            _cts?.Dispose();
            _cts = null;
            _currentBacklogItemId = null;
            _startedAt = null;
        }

        // Clear agent assignment and handle completion via scheduler
        using (var scope = scopeFactory.CreateScope())
        {
            var backlogService = scope.ServiceProvider.GetRequiredService<BacklogService>();
            await backlogService.SetAgentAsync(backlogItemId, null);

            // Let scheduler handle auto-transition based on completion result
            var schedulerService = scope.ServiceProvider.GetRequiredService<SchedulerService>();
            await schedulerService.HandleBacklogAgentCompletionAsync(backlogItemId, completionResult);
        }

        await sse.EmitAgentStatusChangedAsync(GetStatus());
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength) return content;
        return content[..maxLength] + "...";
    }

    /// <summary>
    /// Creates a tool permission handler that intercepts AskUserQuestion tool calls.
    /// When AskUserQuestion is invoked, it stores the question in the database,
    /// emits an SSE event, waits for the user's answer, and returns it to the agent.
    /// </summary>
    private ToolPermissionHandler CreateToolPermissionHandler(
        Guid? taskId,
        Guid? backlogItemId,
        CancellationToken agentCt)
    {
        return async (context, handlerCt) =>
        {
            // Only intercept AskUserQuestion tool
            if (context.ToolName != "AskUserQuestion")
            {
                return PermissionResult.Allow();
            }

            logger.LogInformation(
                "AskUserQuestion tool invoked for {EntityType} {EntityId}",
                taskId.HasValue ? "task" : "backlog item",
                taskId ?? backlogItemId);

            try
            {
                // Parse questions from tool input
                var questions = ParseQuestionsFromInput(context.Input);

                if (questions.Count == 0)
                {
                    logger.LogWarning("AskUserQuestion called with no questions");
                    return PermissionResult.Allow();
                }

                // Create question in database and emit SSE event
                AgentQuestionEntity questionEntity;
                using (var scope = scopeFactory.CreateScope())
                {
                    var questionService = scope.ServiceProvider.GetRequiredService<AgentQuestionService>();
                    questionEntity = await questionService.CreateQuestionAsync(
                        taskId,
                        backlogItemId,
                        context.ToolUseId,
                        questions,
                        handlerCt);
                }

                // Create waiter for user answer
                var tcs = questionWaiter.CreateWaiter(questionEntity.Id);

                // Combine cancellation tokens
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(agentCt, handlerCt);

                SubmitAnswerDto answer;
                try
                {
                    // Wait for user to answer (or timeout/cancellation)
                    answer = await tcs.Task.WaitAsync(linkedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    logger.LogInformation("Question {QuestionId} was cancelled", questionEntity.Id);

                    // Mark as cancelled in database if it was due to abort
                    using var scope = scopeFactory.CreateScope();
                    var questionService = scope.ServiceProvider.GetRequiredService<AgentQuestionService>();
                    await questionService.MarkCancelledAsync(questionEntity.Id, CancellationToken.None);

                    throw;
                }

                // Build response JSON for Claude
                var responseJson = BuildAnswerJson(questions, answer.Answers);

                logger.LogInformation(
                    "Question {QuestionId} answered, returning response to agent",
                    questionEntity.Id);

                return PermissionResult.Allow(responseJson);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error handling AskUserQuestion tool");
                return PermissionResult.Deny($"Error handling question: {ex.Message}");
            }
        };
    }

    /// <summary>
    /// Parses the questions from the tool input JSON.
    /// </summary>
    private static List<AgentQuestionItem> ParseQuestionsFromInput(JsonElement input)
    {
        if (!input.TryGetProperty("questions", out var questionsElement))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<AgentQuestionItem>>(questionsElement.GetRawText(), JsonOptions) ?? [];
    }

    /// <summary>
    /// Builds the answer JSON to send back to Claude.
    /// Claude expects: { "answers": { "0": "selected answer text", ... } }
    /// </summary>
    private static JsonElement BuildAnswerJson(
        IReadOnlyList<AgentQuestionItem> questions,
        IReadOnlyList<QuestionAnswer> answers)
    {
        var answersDict = new Dictionary<string, string>();

        foreach (var answer in answers)
        {
            if (answer.QuestionIndex < 0 || answer.QuestionIndex >= questions.Count)
                continue;

            var question = questions[answer.QuestionIndex];

            // Use custom answer if provided, otherwise build from selected options
            string answerText;
            if (!string.IsNullOrEmpty(answer.CustomAnswer))
            {
                answerText = answer.CustomAnswer;
            }
            else
            {
                var selectedLabels = answer.SelectedOptionIndices
                    .Where(i => i >= 0 && i < question.Options.Count)
                    .Select(i => question.Options[i].Label)
                    .ToList();

                answerText = selectedLabels.Count > 0
                    ? string.Join(", ", selectedLabels)
                    : "No selection";
            }

            answersDict[answer.QuestionIndex.ToString()] = answerText;
        }

        using var doc = JsonDocument.Parse(JsonSerializer.Serialize(new { answers = answersDict }, JsonOptions));
        return doc.RootElement.Clone();
    }
}
