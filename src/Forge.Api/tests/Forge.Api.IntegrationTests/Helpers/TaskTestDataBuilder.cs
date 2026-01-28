namespace Forge.Api.IntegrationTests.Helpers;

public class CreateBacklogItemDtoBuilder
{
    private string _title = "Test Backlog Item";
    private string _description = "Test Description";
    private Priority _priority = Priority.Medium;
    private string? _acceptanceCriteria;

    public CreateBacklogItemDtoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public CreateBacklogItemDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CreateBacklogItemDtoBuilder WithPriority(Priority priority)
    {
        _priority = priority;
        return this;
    }

    public CreateBacklogItemDtoBuilder WithAcceptanceCriteria(string acceptanceCriteria)
    {
        _acceptanceCriteria = acceptanceCriteria;
        return this;
    }

    public CreateBacklogItemDto Build() => new(_title, _description, _priority, _acceptanceCriteria);
}

public class TransitionBacklogItemDtoBuilder
{
    private BacklogItemState _targetState = BacklogItemState.Executing;

    public TransitionBacklogItemDtoBuilder WithTargetState(BacklogItemState state)
    {
        _targetState = state;
        return this;
    }

    public TransitionBacklogItemDto Build() => new(_targetState);
}

public class CreateTaskDtoBuilder
{
    private string _title = "Test Task";
    private string _description = "Test Description";
    private Priority _priority = Priority.Medium;

    public CreateTaskDtoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public CreateTaskDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public CreateTaskDtoBuilder WithPriority(Priority priority)
    {
        _priority = priority;
        return this;
    }

    public CreateTaskDto Build() => new(_title, _description, _priority);
}

public class UpdateTaskDtoBuilder
{
    private string? _title;
    private string? _description;
    private Priority? _priority;

    public UpdateTaskDtoBuilder WithTitle(string title)
    {
        _title = title;
        return this;
    }

    public UpdateTaskDtoBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public UpdateTaskDtoBuilder WithPriority(Priority priority)
    {
        _priority = priority;
        return this;
    }

    public UpdateTaskDto Build() => new(_title, _description, _priority);
}

public class TransitionTaskDtoBuilder
{
    private PipelineState _targetState = PipelineState.Planning;

    public TransitionTaskDtoBuilder WithTargetState(PipelineState state)
    {
        _targetState = state;
        return this;
    }

    public TransitionTaskDto Build() => new(_targetState);
}
