using CloudCrafter.Agent.Models.Configs;
using CloudCrafter.Agent.Models.Deployment.Steps.Params;

namespace CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;

public interface INixpacksHelper
{
    Task<string> DetermineBuildPackAsync(string nixpacksPath);
    Task<string> GetBuildPlanAsync(string fullPath, NixpacksGeneratePlanParams parameters);
    Task<ExecutorResult> BuildDockerImage(NixpacksBuildDockerImageConfig config);
}
