namespace CloudCrafter.Agent.Models.Recipe;

public class DeploymentRecipe
{
    public required string Name { get; init; }
    public required DeploymentRecipeApplicationInfo Application { get; init; }
    public DeploymentRecipeDockerComposeOptions? DockerComposeOptions { get; set; }
    public required DeploymentRecipeDestination Destination { get; init; }
    public required DeploymentBuildOptions BuildOptions { get; init; }
}

public class DeploymentRecipeApplicationInfo
{
    public required Guid Id { get; init; }
}

public class DeploymentRecipeDockerComposeOptions
{
    public string? Base64DockerCompose { get; set; }
    public string? DockerComposeDirectory { get; set; }
}

public class DeploymentRecipeDestination
{
    public required string RootDirectory { get; init; }
}

public class DeploymentBuildOptions
{
    public required List<DeploymentBuildStep> Steps { get; init; }
}

public class DeploymentBuildStep
{
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required DeploymentBuildStepType Type { get; init; }
    public required Dictionary<string, object> Params { get; init; }
}

public enum DeploymentBuildStepType
{
    FetchGitRepository,
    NixpacksDetermineBuildPack,
    NixpacksGeneratePlan,
    NixpacksAlterPlan,
    NixpacksWritePlanToFileSystem,
    NixpacksBuildDockerImage,
    DockerComposeWriteToFileSystem,
    DockerComposeUp,
    DockerValidateNetworksExists,
    ContainerHealthCheck
}
