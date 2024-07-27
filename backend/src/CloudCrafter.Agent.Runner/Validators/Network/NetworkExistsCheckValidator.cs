using CloudCrafter.Agent.Models.Deployment.Steps;
using CloudCrafter.Agent.Models.Deployment.Steps.Params.DockerCompose;
using CloudCrafter.Agent.Models.Deployment.Steps.Params.Network;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment;
using FluentValidation;

namespace CloudCrafter.Agent.Runner.Validators.Network;

public class NetworkExistsCheckValidator : AbstractValidator<NetworkExistsCheckParams>
{
    public NetworkExistsCheckValidator()
    {
        RuleFor(x => x.Networks).NotEmpty();
    }
}

[DeploymentStep(DeploymentBuildStepType.DockerValidateNetworksExists)]
public class DockerComposeUpConfig : IDeploymentStepConfig<NetworkExistsCheckParams>
{
    public IValidator<NetworkExistsCheckParams> Validator => new NetworkExistsCheckValidator();
}
