using CloudCrafter.Agent.Models.Deployment;
using CloudCrafter.Agent.Models.Deployment.Steps;
using CloudCrafter.Agent.Models.Deployment.Steps.Params.Network;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;

namespace CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps.Network;

[DeploymentStep(DeploymentBuildStepType.DockerValidateNetworksExists)]
public class NetworkExistsCheckHandler(IMessagePump pump, IDockerHelper dockerHelper) : IDeploymentStepHandler<NetworkExistsCheckParams>
{
    private readonly IDeploymentLogger _logger = pump.CreateLogger<NetworkExistsCheckHandler>();
    public async Task ExecuteAsync(NetworkExistsCheckParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Checking if provided networks exist");

        var availableNetworks = await dockerHelper.GetNetworks();
        
        var availableNetworksNames = availableNetworks.Select(x => x.Name).ToList();

        var unavailableNetworks = new List<string>();
        
        foreach (var networkName in parameters.Networks)
        {
            if (!availableNetworksNames.Contains(networkName))
            {
                unavailableNetworks.Add(networkName);
            }
        }
        
        if (unavailableNetworks.Any())
        {
            _logger.LogCritical($"Networks {string.Join(", ", unavailableNetworks)} do not exist");
            throw new DeploymentException($"Networks {string.Join(", ", unavailableNetworks)} do not exist");
        }
        
        _logger.LogInfo("All provided networks exist");

    }

    public Task DryRun(NetworkExistsCheckParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Checking if provided networks exist in dry run mode");

        return Task.CompletedTask;
    }
}
