using CloudCrafter.Agent.Models.Docker.Filters;
using Docker.DotNet.Models;

namespace CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;

public interface IDockerHelper
{
    Task<DockerHelperResponseResult> RunCommandInContainer(string containerName, IList<string> commands,
        Action<DockerHelperResponse>? onLog = null);

    Task<ContainerInspectResponse> GetDockerContainer(string containerId);

    Task<IList<ContainerListResponse>> GetContainerFromFilter(DockerContainerFilter filter);
    Task<IList<NetworkResponse>> GetNetworks();
}
