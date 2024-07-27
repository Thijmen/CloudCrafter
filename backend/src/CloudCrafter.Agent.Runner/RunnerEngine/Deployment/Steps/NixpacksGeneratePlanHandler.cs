using CloudCrafter.Agent.Models.Deployment;
using CloudCrafter.Agent.Models.Deployment.Steps;
using CloudCrafter.Agent.Models.Deployment.Steps.Params;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;

namespace CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps;

[DeploymentStep(DeploymentBuildStepType.NixpacksGeneratePlan)]
// ReSharper disable once UnusedType.Global
public class NixpacksGeneratePlanHandler(IMessagePump pump, INixpacksHelper nixpacksHelper)
    : IDeploymentStepHandler<NixpacksGeneratePlanParams>
{
    private readonly IDeploymentLogger _logger = pump.CreateLogger<NixpacksGeneratePlanHandler>();

    public async Task ExecuteAsync(NixpacksGeneratePlanParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Starting nixpacks plan handler");
        var fullPath = context.GetWorkingDirectory() + $"/git/{parameters.Path}";

        var plan = await nixpacksHelper.GetBuildPlanAsync(fullPath, parameters);


        context.SetRecipeResult(RecipeResultKeys.NixpacksBuildPlan, plan);
        _logger.LogInfo("Saved nixpacks plan to to context.");
    }

    public Task DryRun(NixpacksGeneratePlanParams parameters, DeploymentContext context)
    {
        _logger.LogInfo("Generate nixpacks plan handler");

        return Task.CompletedTask;
    }
}
