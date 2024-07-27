using CloudCrafter.Agent.Models.Deployment;
using CloudCrafter.Agent.Models.Deployment.Steps;
using CloudCrafter.Agent.Models.Deployment.Steps.Params.Container;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;

namespace CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps.Container;

[DeploymentStep(DeploymentBuildStepType.ContainerHealthCheck)]
public class ContainerHealthCheckHandler(IMessagePump pump, IDockerHealthCheckHelper dockerHealthCheckHelper)
    : IDeploymentStepHandler<ContainerHealthCheckParams>
{
    private readonly IDeploymentLogger _logger = pump.CreateLogger<ContainerHealthCheckHandler>();

    public async Task ExecuteAsync(ContainerHealthCheckParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Running container health check");

        // This is already validated, but better safe than sorry.
        if (!parameters.Services.Any())
        {
            throw new DeploymentException("Container health check options not found.");
        }


        var dockerComposeServiceContainerIdMap = new Dictionary<string, string>();

        if (parameters.DockerComposeSettings?.FetchServicesFromContext == true)
        {
            dockerComposeServiceContainerIdMap = context.GetRecipeResult<Dictionary<string, string>>(RecipeResultKeys.DockerComposeServices);
        }
        
        var taskDictionary = new Dictionary<string, Task<bool>>();


        foreach (var service in parameters.Services)
        {
            
            var containerId = dockerComposeServiceContainerIdMap.TryGetValue(service.Key, out var value) ? value : service.Key;
            
            var containerIsHealthyForService = dockerHealthCheckHelper.IsHealthyAsync(containerId, service.Value);

            taskDictionary[service.Key] = containerIsHealthyForService;
        }


        await Task.WhenAll(taskDictionary.Values);


        var unhealthyServices = new List<string>();

        foreach (var kvp in taskDictionary)
        {
            var serviceName = kvp.Key;
            var isHealthy = kvp.Value.Result;

            if (!isHealthy)
            {
                unhealthyServices.Add(serviceName);
            }
        }


        if (unhealthyServices.Any())
        {
            var unhealthyServicesString = string.Join(", ", unhealthyServices);
            _logger.LogCritical($"The following containers did not become healthy: {unhealthyServicesString}");
            throw new DeploymentException($"Container health checks failed for services: {unhealthyServicesString}");
        }


        _logger.LogInfo("All provided containers are healthy");
    }

    public Task DryRun(ContainerHealthCheckParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Running container health check in dry run mode");

        return Task.CompletedTask;
    }
}
