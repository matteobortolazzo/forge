using System.Text;
using Claude.CodeSdk;
using Claude.CodeSdk.ContentBlocks;
using Claude.CodeSdk.Messages;
using Forge.Api.Data;
using Forge.Api.Data.Entities;
using Forge.Api.Features.Agents;
using Forge.Api.Features.Events;
using Forge.Api.Features.Scheduler;
using Forge.Api.Features.Tasks;
using Forge.Api.Shared;
using Microsoft.EntityFrameworkCore;

namespace Forge.Api.Features.Agent;

public class AgentRunnerService(
    IServiceScopeFactory scopeFactory,
    ISseService sse,
    IConfiguration configuration,
    IClaudeAgentClientFactory clientFactory,
    IOrchestratorService orchestrator,
    IArtifactParser artifactParser,
    ILogger<AgentRunnerService> logger) : IAgentRunnerService
{
    private readonly Lock _lock = new();
    private CancellationTokenSource? _cts;
    private Guid? _currentTaskId;
    private DateTime? _startedAt;

    public AgentStatusDto GetStatus()
    {
        lock (_lock)
        {
            return new AgentStatusDto(_currentTaskId.HasValue, _currentTaskId, _startedAt);
        }
    }

    public async Task<bool> StartAgentAsync(Guid taskId, string title, string description)
    {
        lock (_lock)
        {
            if (_currentTaskId.HasValue)
            {
                logger.LogWarning("Agent already running on task {TaskId}", _currentTaskId);
                return false;
            }

            _currentTaskId = taskId;
            _startedAt = DateTime.UtcNow;
            _cts = new CancellationTokenSource();
        }

        await sse.EmitAgentStatusChangedAsync(true, taskId, _startedAt);

        // Update task to assign agent
        using (var scope = scopeFactory.CreateScope())
        {
            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
            await taskService.SetAgentAsync(taskId, "claude-agent");
        }

        // Fire and forget the agent execution
        _ = RunAgentAsync(taskId, title, description, _cts.Token);

        return true;
    }

    public async Task<bool> AbortAsync()
    {
        CancellationTokenSource? cts;
        Guid? taskId;

        lock (_lock)
        {
            if (_cts is null || !_currentTaskId.HasValue)
            {
                return false;
            }

            cts = _cts;
            taskId = _currentTaskId;
        }

        logger.LogInformation("Aborting agent for task {TaskId}", taskId);

        // Cancel the token
        await cts.CancelAsync();

        return true;
    }

    private async Task RunAgentAsync(Guid taskId, string title, string description, CancellationToken ct)
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
            resolvedConfig = await orchestrator.SelectAgentAsync(task, workingDirectory);

            logger.LogInformation(
                "Using agent {AgentId} for task {TaskId} in state {State}",
                resolvedConfig.Config.Id, taskId, task.State);

            // Update task context if detected
            var detectedLanguage = task.DetectedLanguage;
            var detectedFramework = task.DetectedFramework;

            var options = new ClaudeAgentOptions
            {
                WorkingDirectory = workingDirectory,
                CliPath = cliPath,
                DangerouslySkipPermissions = true,
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

                await ProcessMessageAsync(taskId, message);
            }

            // Agent completed successfully
            logger.LogInformation("Agent completed task {TaskId} successfully", taskId);
            completionResult = AgentCompletionResult.Success;
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Agent was cancelled for task {TaskId}", taskId);
            await AddLogAsync(taskId, LogType.Info, "Agent execution was cancelled.");
            completionResult = AgentCompletionResult.Cancelled;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent failed for task {TaskId}", taskId);
            await AddLogAsync(taskId, LogType.Error, $"Agent error: {ex.Message}");

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
                await StoreArtifactAsync(taskId, taskState.Value, resolvedConfig, agentOutput.ToString());
            }

            await CleanupAsync(taskId, completionResult);
        }
    }

    private async Task StoreArtifactAsync(
        Guid taskId,
        PipelineState taskState,
        ResolvedAgentConfig resolvedConfig,
        string output)
    {
        try
        {
            // Parse artifact from output
            var parsed = artifactParser.ParseArtifact(output, resolvedConfig.Config);
            if (parsed != null)
            {
                // Store the artifact
                await orchestrator.StoreArtifactAsync(
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

    private async Task ProcessMessageAsync(Guid taskId, IMessage message)
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
                            await AddLogAsync(taskId, logType, text.Text);
                            break;

                        case ToolUseBlock tool:
                            var toolInput = tool.Input.ToString();
                            var content = $"Using {tool.Name}: {TruncateContent(toolInput, 500)}";
                            await AddLogAsync(taskId, LogType.ToolUse, content, tool.Name);
                            break;

                        case ToolResultBlock result:
                            var resultContent = TruncateContent(result.Content, 500);
                            var resultType = result.IsError ? LogType.Error : LogType.ToolResult;
                            await AddLogAsync(taskId, resultType, resultContent);
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
                await AddLogAsync(taskId, LogType.Info, summary);
                break;
        }
    }

    private async Task AddLogAsync(Guid taskId, LogType type, string content, string? toolName = null)
    {
        using var scope = scopeFactory.CreateScope();
        var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
        await taskService.AddLogAsync(taskId, type, content, toolName);
    }

    private async Task CleanupAsync(Guid taskId, AgentCompletionResult completionResult)
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

        await sse.EmitAgentStatusChangedAsync(false, null, null);
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength) return content;
        return content[..maxLength] + "...";
    }
}
