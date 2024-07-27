using CloudCrafter.Agent.Models.Configs;
using CloudCrafter.Agent.Models.Deployment.Steps.Params;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using Microsoft.Extensions.Logging;

namespace CloudCrafter.Agent.Runner.Cli.Helpers;

public class NixpacksHelper(ICommandExecutor executor, ICommandParser parser, ILogger<NixpacksHelper> logger)
    : INixpacksHelper
{
    private const string NixpacksExecutable = "nixpacks";

    public async Task<string> DetermineBuildPackAsync(string nixpacksPath)
    {
        await EnsureNixpacksInstalled();

        var result = await executor.ExecuteAsync(NixpacksExecutable, ["detect", nixpacksPath]);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StdOut))
        {
            throw new DeploymentException($"Could not determine build pack, directory: {nixpacksPath}");
        }


        var parsedResult = parser.ParseSingleOutput(result.StdOut);

        return parsedResult;
    }

    public async Task<string> GetBuildPlanAsync(string fullPath, NixpacksGeneratePlanParams parameters)
    {
        // TODO: Inject build args eventually.
        await EnsureNixpacksInstalled();

        var result = await executor.ExecuteAsync(NixpacksExecutable, ["plan", "-f", "toml", fullPath]);

        if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StdOut))
        {
            throw new DeploymentException($"Could not generate nixpacks plan for directory: '{fullPath}");
        }

        var parsedResult = parser.ParseSingleOutput(result.StdOut);

        return parsedResult;
    }

    public async Task<ExecutorResult> BuildDockerImage(NixpacksBuildDockerImageConfig config)
    {
        await EnsureNixpacksInstalled();


        List<string> baseCommand =
        [
            "build",
            "-c",
            config.PlanPath,
            "--no-error-without-start", // TODO: What does this mean?
            "-n",
            config.ImageName,
        ];

        if (!config.DisableCache)
        {
            baseCommand.Add("--no-cache");
        }

        baseCommand.AddRange([
            config.WorkDir,
            "-o",
            config.WorkDir
        ]);

        var result = await executor.ExecuteAsync(NixpacksExecutable,
            baseCommand);

        if (!result.IsSuccess)
        {
            throw new DeploymentException("Could not export Nixpacks docker image to directory.");
        }


        List<string> baseDockerBuildCommand =
        [
            "build",
            "--network",
            "host",
            "-f",
            $"{config.WorkDir}/.nixpacks/Dockerfile", // TODO: Add build args
            "--progress",
            "plain",
        ];

        if (!config.DisableCache)
        {
            baseDockerBuildCommand.Add("--no-cache");
        }

        baseDockerBuildCommand.AddRange([
            "-t",
            config.ImageName,
            config.WorkDir
        ]);


        var dockerBuild = await executor.ExecuteWithStreamAsync("docker",
            baseDockerBuildCommand, streamResult =>
            {
                // TODO: Make sure to stream this somewhere... 
                logger.LogInformation(streamResult.Log);
            });

        if (!dockerBuild.IsSuccess)
        {
            throw new DeploymentException("Could not build docker image from Nixpacks plan.");
        }

        return dockerBuild;
    }

    private async Task EnsureNixpacksInstalled()
    {
        var result = await executor.ExecuteAsync(NixpacksExecutable, ["-V"]);

        if (!result.IsSuccess)
        {
            throw new DeploymentException("Nixpacks is not installed or version cannot be determined");
        }
    }
}
