using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Forge.E2E.Console.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Forge.E2E.Console.Services;

/// <summary>
/// HTTP client for interacting with the Forge API.
/// </summary>
public sealed class ForgeApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ForgeApiClient> _logger;

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ForgeApiClient(HttpClient http, IOptions<ForgeApiOptions> options, ILogger<ForgeApiClient> logger)
    {
        _http = http;
        _http.BaseAddress = new Uri(options.Value.BaseUrl);
        _logger = logger;
    }

    #region Repositories

    public async Task<RepositoryDto> CreateRepositoryAsync(CreateRepositoryDto dto, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating repository: {Name} at {Path}", dto.Name, dto.Path);
        var response = await _http.PostAsJsonAsync("/api/repositories", dto, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RepositoryDto>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize repository response");
    }

    public async Task<IReadOnlyList<RepositoryDto>> GetRepositoriesAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<RepositoryDto>>("/api/repositories", JsonOptions, ct)
            ?? [];
    }

    public async Task<RepositoryDto> GetRepositoryAsync(Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<RepositoryDto>($"/api/repositories/{id}", JsonOptions, ct)
            ?? throw new InvalidOperationException($"Repository {id} not found");
    }

    #endregion

    #region Backlog Items

    public async Task<BacklogItemDto> CreateBacklogItemAsync(Guid repoId, CreateBacklogItemDto dto, CancellationToken ct = default)
    {
        _logger.LogDebug("Creating backlog item: {Title}", dto.Title);
        var response = await _http.PostAsJsonAsync($"/api/repositories/{repoId}/backlog", dto, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BacklogItemDto>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize backlog item response");
    }

    public async Task<BacklogItemDto> GetBacklogItemAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<BacklogItemDto>($"/api/repositories/{repoId}/backlog/{id}", JsonOptions, ct)
            ?? throw new InvalidOperationException($"Backlog item {id} not found");
    }

    public async Task<BacklogItemDto> TransitionBacklogItemAsync(Guid repoId, Guid id, TransitionBacklogItemDto dto, CancellationToken ct = default)
    {
        _logger.LogDebug("Transitioning backlog item {Id} to {State}", id, dto.TargetState);
        var response = await _http.PostAsJsonAsync($"/api/repositories/{repoId}/backlog/{id}/transition", dto, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BacklogItemDto>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize backlog item response");
    }

    public async Task<BacklogItemDto> StartBacklogAgentAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Starting agent for backlog item {Id}", id);
        var response = await _http.PostAsync($"/api/repositories/{repoId}/backlog/{id}/start-agent", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<BacklogItemDto>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize backlog item response");
    }

    public async Task AbortBacklogAgentAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Aborting agent for backlog item {Id}", id);
        var response = await _http.PostAsync($"/api/repositories/{repoId}/backlog/{id}/abort", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<BacklogItemLogDto>> GetBacklogLogsAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<BacklogItemLogDto>>($"/api/repositories/{repoId}/backlog/{id}/logs", JsonOptions, ct)
            ?? [];
    }

    public async Task<IReadOnlyList<ArtifactDto>> GetBacklogArtifactsAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<ArtifactDto>>($"/api/repositories/{repoId}/backlog/{id}/artifacts", JsonOptions, ct)
            ?? [];
    }

    public async Task<IReadOnlyList<HumanGateDto>> GetBacklogGatesAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<HumanGateDto>>($"/api/repositories/{repoId}/backlog/{id}/gates", JsonOptions, ct)
            ?? [];
    }

    #endregion

    #region Tasks

    public async Task<IReadOnlyList<TaskDto>> GetTasksAsync(Guid repoId, Guid? backlogItemId = null, CancellationToken ct = default)
    {
        var url = $"/api/repositories/{repoId}/tasks";
        if (backlogItemId.HasValue)
        {
            url += $"?backlogItemId={backlogItemId.Value}";
        }
        return await _http.GetFromJsonAsync<IReadOnlyList<TaskDto>>(url, JsonOptions, ct)
            ?? [];
    }

    public async Task<TaskDto> GetTaskAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<TaskDto>($"/api/repositories/{repoId}/tasks/{id}", JsonOptions, ct)
            ?? throw new InvalidOperationException($"Task {id} not found");
    }

    public async Task<TaskDto> StartTaskAgentAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Starting agent for task {Id}", id);
        var response = await _http.PostAsync($"/api/repositories/{repoId}/tasks/{id}/start-agent", null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<TaskDto>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize task response");
    }

    public async Task AbortTaskAgentAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        _logger.LogDebug("Aborting agent for task {Id}", id);
        var response = await _http.PostAsync($"/api/repositories/{repoId}/tasks/{id}/abort", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<TaskLogDto>> GetTaskLogsAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<TaskLogDto>>($"/api/repositories/{repoId}/tasks/{id}/logs", JsonOptions, ct)
            ?? [];
    }

    public async Task<IReadOnlyList<ArtifactDto>> GetTaskArtifactsAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<ArtifactDto>>($"/api/repositories/{repoId}/tasks/{id}/artifacts", JsonOptions, ct)
            ?? [];
    }

    public async Task<IReadOnlyList<HumanGateDto>> GetTaskGatesAsync(Guid repoId, Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<HumanGateDto>>($"/api/repositories/{repoId}/tasks/{id}/gates", JsonOptions, ct)
            ?? [];
    }

    #endregion

    #region Human Gates

    public async Task<IReadOnlyList<HumanGateDto>> GetPendingGatesAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<IReadOnlyList<HumanGateDto>>("/api/gates/pending", JsonOptions, ct)
            ?? [];
    }

    public async Task<HumanGateDto> GetGateAsync(Guid id, CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<HumanGateDto>($"/api/gates/{id}", JsonOptions, ct)
            ?? throw new InvalidOperationException($"Gate {id} not found");
    }

    public async Task<HumanGateDto> ResolveGateAsync(Guid id, ResolveHumanGateDto dto, CancellationToken ct = default)
    {
        _logger.LogDebug("Resolving gate {Id} with status {Status}", id, dto.Status);
        var response = await _http.PostAsJsonAsync($"/api/gates/{id}/resolve", dto, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HumanGateDto>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize gate response");
    }

    #endregion

    #region Agent Questions

    public async Task<AgentQuestionDto?> GetPendingQuestionAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/agent/questions/pending", ct);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        response.EnsureSuccessStatusCode();

        // API returns empty body when no pending question
        var content = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return JsonSerializer.Deserialize<AgentQuestionDto>(content, JsonOptions);
    }

    public async Task<AgentQuestionDto> AnswerQuestionAsync(Guid id, SubmitAnswerDto dto, CancellationToken ct = default)
    {
        _logger.LogDebug("Answering question {Id}", id);
        var response = await _http.PostAsJsonAsync($"/api/agent/questions/{id}/answer", dto, JsonOptions, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AgentQuestionDto>(JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to deserialize question response");
    }

    #endregion

    #region Scheduler

    public async Task<SchedulerStatusDto> GetSchedulerStatusAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<SchedulerStatusDto>("/api/scheduler/status", JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to get scheduler status");
    }

    public async Task EnableSchedulerAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Enabling scheduler");
        var response = await _http.PostAsync("/api/scheduler/enable", null, ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task DisableSchedulerAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Disabling scheduler");
        var response = await _http.PostAsync("/api/scheduler/disable", null, ct);
        response.EnsureSuccessStatusCode();
    }

    #endregion

    #region Agent

    public async Task<AgentStatusDto> GetAgentStatusAsync(CancellationToken ct = default)
    {
        return await _http.GetFromJsonAsync<AgentStatusDto>("/api/agent/status", JsonOptions, ct)
            ?? throw new InvalidOperationException("Failed to get agent status");
    }

    #endregion
}
