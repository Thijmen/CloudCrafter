namespace CloudCrafter.Agent.Models.Configs;

public class NixpacksBuildDockerImageConfig
{
    public required string PlanPath { get; init; }
    public required string WorkDir { get; init; }
    public required string ImageName { get; init; }
    public required bool DisableCache { get; init; }
}
