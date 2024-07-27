using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.DockerCompose.Engine.Yaml;
using CloudCrafter.Shared.Utils;

namespace CloudCrafter.Agent.Console.IntegrationTests.RunnerEngine.Deployment.Steps;

public abstract class HandlerBaseCase
{
    public DeploymentContext GetContext()
    {
        var recipe = GetRecipe();
        var context = new DeploymentContext(recipe);

        return context;
    }

    public DeploymentRecipe GetRecipe()
    {
        var imageRepository = "test-image";
        var imageTag = "testing";

        var editor = new DockerComposeEditor();
        var service = editor.AddService("frontend");
        service.SetImage(imageRepository, imageTag);
        service.AddExposedPort(3000, 3000);
        
        
         var recipe = new DeploymentRecipe
            {
                Name = "My Application",
                Application = new()
                {
                    Id = Guid.NewGuid()
                },
                Destination =
                    new DeploymentRecipeDestination { RootDirectory = "/tmp/cloudcrafter-testing/" + RandomGenerator.String() },
                DockerComposeOptions =
                    new DeploymentRecipeDockerComposeOptions
                    {
                        Base64DockerCompose = editor.ToBase64(),
                        // In production envs, this will have the application stack guid in it
                        DockerComposeDirectory =
                            "/tmp/cloudcrafter-data/my-application"
                    },
                BuildOptions = new DeploymentBuildOptions
                {
                    Steps = new List<DeploymentBuildStep>
                    {
                        new()
                        {
                            Name = "Fetch git",
                            Description = "Fetch the git application",
                            Type = DeploymentBuildStepType.FetchGitRepository,
                            Params =
                                new Dictionary<string, object>
                                {
                                    { "repo", "https://github.com/cloudcrafter-oss/demo-examples.git" },
                                    { "commit", "HEAD" }
                                }
                        },
                        new()
                        {
                            Name = "Determine Buildpack",
                            Description = "Determine the buildpack",
                            Type = DeploymentBuildStepType.NixpacksDetermineBuildPack,
                            Params = new Dictionary<string, object> { { "path", "nixpacks-node-server" } }
                        },
                        new()
                        {
                            Name = "Generate Build plan",
                            Description = "Generate the build plan",
                            Type = DeploymentBuildStepType.NixpacksGeneratePlan,
                            Params = new Dictionary<string, object> { { "path", "nixpacks-node-server" } }
                        },
                        new()
                        {
                            Name = "Alter plan",
                            Description = "Alter plan",
                            Type = DeploymentBuildStepType.NixpacksAlterPlan,
                            Params =
                                new Dictionary<string, object> { { "packages", new List<string> { "iputils-ping" } } }
                        },
                        new()
                        {
                            Name = "Write plan to filesystem",
                            Description = "Write plan to filesystem",
                            Type = DeploymentBuildStepType.NixpacksWritePlanToFileSystem,
                            Params = new Dictionary<string, object> { { "path", "nixpacks-node-server" } }
                        },
                        new()
                        {
                            Name = "Build Nixpacks docker image",
                            Description = "Builds Nixpacks docker image",
                            Type = DeploymentBuildStepType.NixpacksBuildDockerImage,
                            Params = new Dictionary<string, object>
                            {
                                { "path", "nixpacks-node-server" },
                                { "image", imageRepository },
                                { "tag", imageTag },
                                { "disableCache", true }
                            }
                        },
                        new()
                        {
                            Name = "Write docker compose file",
                            Description = "Write docker compose file",
                            Type = DeploymentBuildStepType.DockerComposeWriteToFileSystem,
                            Params = new() { { "dockerComposeFile", "docker-compose.yml" } }
                        }
                    }
                }
            };

            return recipe;
    }
}
