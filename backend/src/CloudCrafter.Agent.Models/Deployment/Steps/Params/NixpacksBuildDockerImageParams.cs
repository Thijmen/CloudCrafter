using CloudCrafter.Agent.Models.Recipe;

namespace CloudCrafter.Agent.Models.Deployment.Steps.Params;

[DeploymentStep(DeploymentBuildStepType.NixpacksBuildDockerImage)]
// ReSharper disable once ClassNeverInstantiated.Global
public class NixpacksBuildDockerImageParams : NixpacksBaseParams
{
    public string Image { get; init; } = string.Empty;
    public string Tag { get; init; } = string.Empty;
    public bool? DisableCache { get; init; } = false;
    public Dictionary<string, object>? Env { get; init; }
}
