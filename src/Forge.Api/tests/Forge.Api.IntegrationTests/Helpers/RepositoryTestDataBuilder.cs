namespace Forge.Api.IntegrationTests.Helpers;

public class CreateRepositoryDtoBuilder
{
    private string _name = "Test Repository";
    private string _path = ForgeWebApplicationFactory.ProjectRoot;

    public CreateRepositoryDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreateRepositoryDtoBuilder WithPath(string path)
    {
        _path = path;
        return this;
    }

    public CreateRepositoryDto Build() => new(_name, _path);
}

public class UpdateRepositoryDtoBuilder
{
    private string? _name;

    public UpdateRepositoryDtoBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public UpdateRepositoryDto Build() => new(_name);
}
