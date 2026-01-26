namespace Forge.Api.IntegrationTests.Helpers;

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
