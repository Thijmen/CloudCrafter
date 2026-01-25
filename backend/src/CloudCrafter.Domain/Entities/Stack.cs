using CloudCrafter.Domain.Common;
using CloudCrafter.Domain.Entities.Jobs;

namespace CloudCrafter.Domain.Entities;

public class Stack : BaseAuditableEntity
{
    public required string Name { get; set; }
    public Environment Environment { get; set; } = null!;
    public required Guid EnvironmentId { get; set; }
    public Server? Server { get; set; }
    public required Guid ServerId { get; set; }

    public required StackBuildPack BuildPack { get; init; }
    public ApplicationSource? Source { get; set; }

    public StackDockerComposeData DockerComposeData { get; set; } = new();

    public List<Deployment> Deployments { get; set; } = new();
    public List<StackService> Services { get; set; } = new();
    public List<StackEnvironmentVariable> EnvironmentVariables { get; set; } = new();
    public List<StackEnvironmentVariableGroup> EnvironmentVariableGroups { get; set; } = new();

    public required StackHealthEntity HealthStatus { get; init; }
    public required string? Description { get; set; }
}

public enum StackBuildPack
{
    Nixpacks = 0,
    DockerCompose = 1,
}
