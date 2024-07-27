using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using Docker.DotNet;

namespace CloudCrafter.Agent.Runner.Cli.Helpers;

public class DockerClientProvider : IDockerClientProvider
{
    public IDockerClient GetClient()
    {
        return new DockerClientConfiguration()
            .CreateClient();
    }
}
