using System.Text;
using CloudCrafter.Agent.Models.Deployment;
using CloudCrafter.Agent.Models.Deployment.Steps;
using CloudCrafter.Agent.Models.Deployment.Steps.Params;
using CloudCrafter.Agent.Models.Deployment.Steps.Params.DockerCompose;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;

namespace CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps.DockerCompose;

[DeploymentStep(DeploymentBuildStepType.DockerComposeWriteToFileSystem)]
// ReSharper disable once UnusedType.Global
public class DockerComposeWriteToFileSystemHandler(IMessagePump pump, IFileSystemHelper fileSystemHelper)
    : IDeploymentStepHandler<DockerComposeWriteToFileSystemParams>
{
    private readonly IDeploymentLogger _logger = pump.CreateLogger<DockerComposeWriteToFileSystemHandler>();

    public async Task ExecuteAsync(DockerComposeWriteToFileSystemParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Writing docker compose file to the file system");

        if (context.Recipe.DockerComposeOptions == null ||
            string.IsNullOrWhiteSpace(context.Recipe.DockerComposeOptions.Base64DockerCompose)
            || string.IsNullOrWhiteSpace(context.Recipe.DockerComposeOptions.DockerComposeDirectory))
        {
            throw new DeploymentException("Docker compose options not found - cannot write docker compose file.");
        }

        await fileSystemHelper.EnsureDirectoryExists(context.Recipe.DockerComposeOptions.DockerComposeDirectory);
        
        var decodedBase64Content = Convert.FromBase64String(context.Recipe.DockerComposeOptions.Base64DockerCompose);
        var decodedContent = Encoding.UTF8.GetString(decodedBase64Content); // TODO: Extract this to somewhere
        
        var fullPath = $"{context.Recipe.DockerComposeOptions.DockerComposeDirectory}/{parameters.DockerComposeFile}";
        
        await fileSystemHelper.WriteFile(fullPath, decodedContent);

        _logger.LogInfo($"Successfully wrote docker compose file to {fullPath}");
    }

    public Task DryRun(DockerComposeWriteToFileSystemParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Writing docker compose file to the file system");

        return Task.CompletedTask;
    }
}
