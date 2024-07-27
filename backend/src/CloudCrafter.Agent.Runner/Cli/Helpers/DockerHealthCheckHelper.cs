using System.Text.Json;
using System.Text.Json.Serialization;
using CloudCrafter.Agent.Models.Deployment.Steps.Params.Container;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.Exceptions;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace CloudCrafter.Agent.Runner.Cli.Helpers;

public class DockerHealthCheckHelper(IDockerHelper dockerHelper, ILogger<DockerHealthCheckHelper> logger)
    : IDockerHealthCheckHelper
{
    private const string HELPER_LOCATION = "/tmp/health_check.sh";

    private const string BASH_COMMAND_HELPER = """
                                               #!/bin/bash
                                               check_response() {
                                                   URL=$1
                                                   
                                                   # Make the curl request and capture the response code and body
                                                   response=$(curl -s -w "%{http_code}" -o /tmp/curl_body.txt $URL)
                                                   curl_exit_status=$?
                                                   response_code=$(tail -n1 <<< "$response")
                                                   response_body=$(cat /tmp/curl_body.txt)
                                               
                                                   # Check if curl command failed
                                                   if [ $curl_exit_status -ne 0 ]; then
                                                       echo "Error: curl command failed with status $curl_exit_status."
                                                       exit $curl_exit_status
                                                   fi
                                                   
                                                   # Check if jq is installed
                                                   if command -v jq &> /dev/null; then
                                                       # Create JSON output with jq
                                                       json_output=$(jq -n --arg code "$response_code" --arg body "$response_body" '{response_code: $code, response_body: $body}')
                                                   else
                                                       # Create JSON output manually
                                                       json_output="{\"response_code\":\"$response_code\"}"
                                                   fi
                                                   
                                                   # Output the JSON
                                                   echo "$json_output"
                                               }

                                               # Check if a URL is provided
                                               if [ -z "$1" ]; then
                                                   echo "Usage: $0 <URL>"
                                                   exit 1
                                               fi

                                               # Call the function with the provided URL
                                               check_response $1

                                               """;

    private class HealthCheckOutput
    {
        [JsonPropertyName("response_code")] public string ResponseCode { get; init; } = string.Empty;
        [JsonPropertyName("response_body")] public string? ResponseBody { get; init; }
    }


    public async Task<bool> IsHealthyAsync(string containerId, ContainerHealthCheckParamsOptions options)
    {
        var retry = Policy<bool>
            .Handle<Exception>()
            .OrResult(result => !result)
            .WaitAndRetryAsync(
                retryCount: options.Retries.GetValueOrDefault(1),
                sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (result, timeSpan, retryCount, context) =>
                {
                    logger.LogWarning("Attempt {RetryCount} failed. Waiting {TimeSpan} before next retry.", retryCount,
                        timeSpan);
                }
            );

        return await retry.ExecuteAsync(async () => await RunHealthCheck(containerId, options));
    }

    private async Task<bool> RunHealthCheck(string containerId, ContainerHealthCheckParamsOptions options)
    {
        var container = await dockerHelper.GetDockerContainer(containerId);

        if (options.CheckForDockerHealth.GetValueOrDefault())
        {
            var containerIsHealthy = CheckDockerHealth(containerId, container);

            if (!containerIsHealthy)
            {
                return false;
            }
        }

        await WriteHealthCheckScript(containerId);

        var url = $"{options.HttpSchema}://{options.HttpHost}:{options.HttpPort}{options.HttpPath}";
        
        logger.LogInformation("Running health check for container {ContainerId} with URL {Url}", containerId, url);
        var result = await dockerHelper.RunCommandInContainer(containerId, [
            "sh",
            "-c",
            $"bash {HELPER_LOCATION} {url}"
        ]);

        if (result.ExitCode != 0)
        {
            logger.LogCritical("Health check failed for container {ContainerId}, exitcode: {ExitCode}", containerId,
                result.ExitCode);

            return false;
        }

        if (string.IsNullOrWhiteSpace(result.StdOut))
        {
            logger.LogCritical("No response output for healthcheck for container {ContainerId}", containerId);
            return false;
        }

        var healthCheckOutput = JsonSerializer.Deserialize<HealthCheckOutput>(result.StdOut);

        if (healthCheckOutput == null)
        {
            logger.LogCritical("Could not parse health check output for container {ContainerId}", containerId);
            return false;
        }

        if (healthCheckOutput.ResponseCode != options.ExpectedResponseCode.ToString())
        {
            logger.LogCritical(
                "Health check failed for container {ContainerId}, expected response code {ExpectedResponseCode} but got {ResponseCode}",
                containerId, options.ExpectedResponseCode, healthCheckOutput.ResponseCode);
            return false;
        }

        return true;
    }

    private async Task WriteHealthCheckScript(string containerId)
    {
        var scriptLocation = HELPER_LOCATION;
        var hasScript = await dockerHelper.RunCommandInContainer(containerId, [
            "sh",
            "-c",
            $"if [ ! -f \"{scriptLocation}\" ]; then echo \"File does not exist.\"; exit 1; fi\necho \"File exists.\""
        ]);

        if (hasScript.ExitCode == 0)
        {
            // File already exists
            return;
        }


        var base64Command =
            Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(BASH_COMMAND_HELPER.Replace("\r\n", "\n")));

        var writeBase64File = "echo '" + base64Command + "' > /tmp/health_check_base64.txt";


        var result = await dockerHelper.RunCommandInContainer(containerId, [
            "sh",
            "-c",
            $"{writeBase64File}"
        ]);

        if (result.ExitCode != 0)
        {
            throw new AgentDockerException($"Error writing health check command to {containerId}");
        }

        var execCommand =
            $"cat /tmp/health_check_base64.txt | base64 -d | tee {scriptLocation} && rm /tmp/health_check_base64.txt && chmod +x {scriptLocation}";

        result = await dockerHelper.RunCommandInContainer(containerId, [
            "sh",
            "-c",
            $"{execCommand}"
        ]);

        if (result.ExitCode != 0)
        {
            throw new AgentDockerException($"Error making health check command writable for container: {containerId}");
        }
    }


    private bool CheckDockerHealth(string containerId, ContainerInspectResponse container)
    {
        if (container.State?.Health == null)
        {
            throw new AgentDockerException(
                "Provided container does not have health check enabled but check options has CheckForDockerHealth = true.");
        }

        var containerIsHealthy = container.State.Health.Status == "healthy";

        if (containerIsHealthy is false)
        {
            logger.LogCritical("Container {ContainerId} is not healthy, current status: {Status}", containerId,
                container.State.Health.Status);
        }


        return containerIsHealthy;
    }
}
