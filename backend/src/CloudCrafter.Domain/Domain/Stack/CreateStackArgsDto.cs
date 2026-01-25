using System.Text.Json.Serialization;

namespace CloudCrafter.Domain.Domain.Stack;

public class CreateStackArgsDto
{
    public required string Name { get; init; }
    public required Guid EnvironmentId { get; set; }
    public required Guid ServerId { get; set; }

    public required CreateStackBuildOption BuildOption { get; init; }

    public CreateStackPublicGitRepo? PublicGit { get; set; }
    public CreateStackGithubAppDto? GithubApp { get; set; }

    public bool IsPublicGitRepo()
    {
        return PublicGit != null && !string.IsNullOrWhiteSpace(PublicGit.GitRepository);
    }

    public bool IsGithubApp()
    {
        return GithubApp != null
            && !string.IsNullOrWhiteSpace(GithubApp.RepositoryId)
            && !string.IsNullOrWhiteSpace(GithubApp.Branch);
    }
}

public class CreateStackPublicGitRepo
{
    public required string GitRepository { get; set; }
    public required string Branch { get; set; }
    public required string Path { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CreateStackBuildOption
{
    Nixpacks = 0,
    DockerCompose = 1,
}

public class CreateStackGithubAppDto
{
    public required string RepositoryId { get; set; }
    public required string Branch { get; set; }
    public required Guid ProviderId { get; set; }

    /// <summary>
    ///     Only a read-only property, Repository Id is leading
    /// </summary>
    public required string Repository { get; set; }

    public required string Path { get; set; }
}
