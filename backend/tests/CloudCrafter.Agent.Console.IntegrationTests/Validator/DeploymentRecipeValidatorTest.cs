using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CloudCrafter.Agent.Console.IntegrationTests.Validator;

public class DeploymentRecipeValidatorTest
{
    private DeploymentService _deploymentService;

    [SetUp]
    public void Setup()
    {
        var builder = Program.CreateHostBuilder([]);
        var host = builder.Build();
        _deploymentService = host.Services.GetRequiredService<DeploymentService>();
    }

    [Test]
    public async Task ShouldHaveAValidRecipe()
    {
        var recipe = new DeploymentRecipe
        {
            Name = "My Application",
            Application = new()
            {
                Id = Guid.NewGuid()
            },
            Destination =
                new DeploymentRecipeDestination { RootDirectory = "/tmp/cloudcrafter/" },
            BuildOptions = new DeploymentBuildOptions
            {
                Steps = new List<DeploymentBuildStep>()
                {
                    new DeploymentBuildStep()
                    {
                        Name = "dummy",
                        Description = "description",
                        Type = DeploymentBuildStepType.FetchGitRepository,
                        Params =
                            new Dictionary<string, object>
                            {
                                { "repo", "https://github.com/cloudcrafter-oss/demo-examples.git" },
                                { "commit", "HEAD" }
                            }
                    }
                }
            }
        };

        await _deploymentService.ValidateRecipe(recipe);

        // If no exception is thrown, the recipe is valid
        true.Should().BeTrue();
    }
    
    [Test]
    public void ShouldNotBeValidRecipeBecauseThereAreNoBuildOptionSteps()
    {
        var recipe = new DeploymentRecipe
        {
            Name = "My Application",
            Application = new()
            {
                Id = Guid.NewGuid()
            },
            Destination =
                new DeploymentRecipeDestination { RootDirectory = "/tmp/cloudcrafter/" },
            BuildOptions = new DeploymentBuildOptions { Steps = new List<DeploymentBuildStep>() }
        };
        
        var exception = Assert.ThrowsAsync<ValidationException>(() => ((DeploymentService)_deploymentService).ValidateRecipe(recipe));
        exception.Should().NotBeNull();

        exception.Errors.Count(error => error.PropertyName == "BuildOptions.Steps")
            .Should().BeGreaterThan(0);
    }

    [Test]
    public void ShouldNotBeValidRecipeWhenDockerComposeOptionsHasNoBase64()
    {
        var recipe = new DeploymentRecipe
        {
            Name = "My Application",
            Application = new()
            {
                Id = Guid.NewGuid()
            },
            Destination =
                new DeploymentRecipeDestination { RootDirectory = "/tmp/cloudcrafter/" },
            DockerComposeOptions = new() { },
            BuildOptions = new DeploymentBuildOptions { Steps = new List<DeploymentBuildStep>() }
        };
        
        var exception = Assert.ThrowsAsync<ValidationException>(() => ((DeploymentService)_deploymentService).ValidateRecipe(recipe));

        exception.Should().NotBeNull();
        exception.Errors.Count(error => error.PropertyName == "DockerComposeOptions.Base64DockerCompose")
            .Should().BeGreaterThan(0);
        
    }
}
