using Docker.DotNet;

namespace CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;

public interface IDockerClientProvider
{
    IDockerClient GetClient();
}
