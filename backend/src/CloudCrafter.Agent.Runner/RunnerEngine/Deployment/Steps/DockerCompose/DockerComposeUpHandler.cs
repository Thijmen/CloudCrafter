using CloudCrafter.Agent.Models.Deployment;
using CloudCrafter.Agent.Models.Deployment.Steps;
using CloudCrafter.Agent.Models.Deployment.Steps.Params.DockerCompose;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;

namespace CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps.DockerCompose;

[DeploymentStep(DeploymentBuildStepType.DockerComposeUp)]
public class DockerComposeUpHandler(IMessagePump pump, IDockerComposeHelper dockerComposeHelper)
    : IDeploymentStepHandler<DockerComposeUpParams>
{
    private readonly IDeploymentLogger _logger = pump.CreateLogger<DockerComposeUpHandler>();

    public async Task ExecuteAsync(DockerComposeUpParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Running docker compose up");

        if (context.Recipe.DockerComposeOptions == null ||
            string.IsNullOrWhiteSpace(context.Recipe.DockerComposeOptions.DockerComposeDirectory))
        {
            throw new DeploymentException("Docker compose options not found - cannot write docker compose file.");
        }

        var dockerComposeFile =
            $"{context.Recipe.DockerComposeOptions.DockerComposeDirectory}/{parameters.DockerComposeFile}";

        var result = await dockerComposeHelper.UpAsync(dockerComposeFile);

        if (!result.IsSuccess)
        {
            throw new DeploymentException("Failed to run docker compose up, see logs for more information.");
        }

        if (parameters.StoreServiceNames.GetValueOrDefault())
        {
            var services = await dockerComposeHelper.GetDockerComposeServices(dockerComposeFile);

            context.SetRecipeResult(RecipeResultKeys.DockerComposeServices, services);
        }
    }

    public Task DryRun(DockerComposeUpParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Running docker compose up in dry run mode");

        return Task.CompletedTask;
    }
}
