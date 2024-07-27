using System.Text.Json;
using System.Text.Json.Serialization;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.Exceptions;
using Microsoft.Extensions.Logging;

namespace CloudCrafter.Agent.Runner.Cli.Helpers;

public class DockerComposeHelper(
    ICommandExecutor executor,
    ICommandParser commandParser,
    ILogger<DockerComposeHelper> logger) : IDockerComposeHelper
{
    public async Task<ExecutorResult> UpAsync(string dockerComposeFile, Action<ExecutorStreamResult>? onLog = null)
    {
        var result = await executor.ExecuteWithStreamAsync("docker", [
            "compose",
            "-f",
            dockerComposeFile,
            "up",
            "-d"
        ], streamResult =>
        {
            var errorAlikeMessages = new List<string> { "error", "fail" };
            // Docker compose up logs to stdout, so we can log it as info
            var logToStdErr = errorAlikeMessages.Any(streamResult.Log.Contains);
            if (logToStdErr)
            {
                logger.LogError(streamResult.Log);
            }
            else
            {
                logger.LogInformation(streamResult.Log);
            }

            onLog?.Invoke(streamResult with { IsStdErr = logToStdErr });
        });

        return result;
    }

    public async Task<string> GetDockerContainerIdForService(string dockerComposeFile, string serviceName)
    {
        var result = await GetDockerComposeServices(dockerComposeFile);

        if (!result.TryGetValue(serviceName, out var service))
        {
            throw new AgentDockerException(
                $"Service {serviceName} not found in docker compose file: {dockerComposeFile}");
        }

        return service;
    }

    public async Task<Dictionary<string, string>> GetDockerComposeServices(string dockerComposeFile)
    {
        try
        {
            var result = await executor.ExecuteAsync("docker", [
                "compose",
                "-f",
                dockerComposeFile,
                "ps",
                "--format",
                "json"
            ]);

            if (string.IsNullOrWhiteSpace(result.StdOut))
            {
                throw new AgentDockerException("No output for given command, file: " + dockerComposeFile);
            }

            var jsonData = commandParser.ParseSingleOutput(result.StdOut);

            var services = jsonData.Split("\n");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            var dictionary = new Dictionary<string, string>();

            foreach (var serviceAsJson in services)
            {
                var container = JsonSerializer.Deserialize<DockerComposeDockerComposePsOutput>(serviceAsJson, options);

                if (container == null)
                {
                    throw new AgentDockerException("Could not deserialize docker compose ps output, file: " +
                                                   dockerComposeFile);
                }

                dictionary.Add(container.Service, container.ID);
            }

            return dictionary;
        }
        catch (AgentDockerException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new AgentDockerException(
                $"Failed to get docker container names for services, file: {dockerComposeFile}", ex);
        }
    }
}

public class DockerComposeDockerComposePsOutput
{
    [JsonPropertyName("Name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("Service")] public string Service { get; set; } = string.Empty;
    [JsonPropertyName("ID")] public string ID { get; set; } = string.Empty;
}
