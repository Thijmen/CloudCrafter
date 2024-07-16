﻿namespace CloudCrafter.Agent.Models.Recipe;

public class DeploymentRecipe
{
    public required string Name { get; init; }
    public required DeploymentRecipeSource Source { get; init; }
    public required DeploymentRecipeDestination Destination { get; init; }
    public required DeploymentBuildOptions BuildOptions { get; init; }
}

public class DeploymentRecipeDestination
{
    public required string RootDirectory { get; init; }
}

public class DeploymentRecipeSource
{
    public DeploymentSourceGit? Git { get; init; }
}

public class DeploymentSourceGit
{
    public required string Repository { get; init; }
    public required string Commit { get; init; }
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
}

public enum DeploymentBuildStepType
{
    FetchGitRepository,
    BuildDockerImage
}
