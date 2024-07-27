using CloudCrafter.Agent.Models.Recipe;

namespace CloudCrafter.Agent.Models.Deployment.Steps.Params.Network;
[DeploymentStep(DeploymentBuildStepType.DockerValidateNetworksExists)]
public class NetworkExistsCheckParams
{
    public List<string> Networks { get; init; } = new();
}
