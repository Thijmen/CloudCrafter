namespace CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;

public interface IDockerComposeHelper
{
    Task<ExecutorResult> UpAsync(string dockerComposeFile, Action<ExecutorStreamResult>? onLog = null);
    Task<string> GetDockerContainerIdForService(string dockerComposeFile, string serviceName);
    Task<Dictionary<string, string>> GetDockerComposeServices(string dockerComposeFile);
}
