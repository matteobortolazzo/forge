using Claude.CodeSdk;
using Claude.CodeSdk.ContentBlocks;
using Claude.CodeSdk.Messages;
using Forge.Api.Features.Events;
using Forge.Api.Features.Tasks;
using Forge.Api.Shared;

namespace Forge.Api.Features.Agent;

public class AgentRunnerService(
    IServiceScopeFactory scopeFactory,
    ISseService sse,
    IConfiguration configuration,
    ILogger<AgentRunnerService> logger)
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
        try
        {
            var workingDirectory = configuration["REPOSITORY_PATH"] ?? Environment.CurrentDirectory;
            var cliPath = configuration["CLAUDE_CODE_PATH"];

            var options = new ClaudeAgentOptions
            {
                WorkingDirectory = workingDirectory,
                CliPath = cliPath,
                DangerouslySkipPermissions = true,
                MaxTurns = 50
            };

            var prompt = $"""
                Task: {title}

                Description:
                {description}

                Please complete this task. Be thorough and provide updates on your progress.
                """;

            await using var client = new ClaudeAgentClient(options);

            await foreach (var message in client.QueryStreamAsync(prompt, ct: ct))
            {
                await ProcessMessageAsync(taskId, message);
            }

            // Agent completed successfully
            logger.LogInformation("Agent completed task {TaskId} successfully", taskId);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Agent was cancelled for task {TaskId}", taskId);
            await AddLogAsync(taskId, LogType.Info, "Agent execution was cancelled.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Agent failed for task {TaskId}", taskId);
            await AddLogAsync(taskId, LogType.Error, $"Agent error: {ex.Message}");

            // Set error on task
            using var scope = scopeFactory.CreateScope();
            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
            await taskService.SetErrorAsync(taskId, ex.Message);
        }
        finally
        {
            await CleanupAsync(taskId);
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

    private async Task CleanupAsync(Guid taskId)
    {
        lock (_lock)
        {
            _cts?.Dispose();
            _cts = null;
            _currentTaskId = null;
            _startedAt = null;
        }

        // Clear agent assignment
        using (var scope = scopeFactory.CreateScope())
        {
            var taskService = scope.ServiceProvider.GetRequiredService<TaskService>();
            await taskService.SetAgentAsync(taskId, null);
        }

        await sse.EmitAgentStatusChangedAsync(false, null, null);
    }

    private static string TruncateContent(string content, int maxLength)
    {
        if (content.Length <= maxLength) return content;
        return content[..maxLength] + "...";
    }
}
