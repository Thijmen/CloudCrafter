using CloudCrafter.Agent.Models.Deployment.Steps.Params.Container;

namespace CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;

public interface IDockerHealthCheckHelper
{
    Task<bool> IsHealthyAsync(string containerId, ContainerHealthCheckParamsOptions options);
}
