using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.DockerCompose.Engine.Yaml;
using CloudCrafter.DockerCompose.Shared.Labels;
using CloudCrafter.Shared.Utils;
using MediatR;

namespace CloudCrafter.Agent.Runner.Commands;

public static class GetDummyDeployment
{
    public record Query(string ImageRepository, string ImageTag, Guid ApplicationId) : IRequest<DeploymentRecipe>;

    private class Handler : IRequestHandler<Query, DeploymentRecipe>
    {
        public Task<DeploymentRecipe> Handle(Query request, CancellationToken cancellationToken)
        {
            var imageRepository = request.ImageRepository;
            var imageTag = request.ImageTag;

            var dockerComposeEditor = new DockerComposeEditor();

            var network = dockerComposeEditor.AddNetwork("cloudcrafter")
                .SetNetworkName("cloudcrafter")
                .SetIsExternalNetwork();

            
            var service = dockerComposeEditor.AddService("frontend");
            service.SetImage(imageRepository, imageTag);

            service.AddNetwork(network);

            var labelService = new DockerComposeLabelService();
            labelService.AddLabel(LabelFactory.GenerateApplicationLabel(request.ApplicationId));
            labelService.AddTraefikLabels(new()
            {
                Rule = "Host(`frontend.127.0.0.1.sslip.io`)",
                Service = "frontend",
                LoadBalancerPort = 3000
            });
            service.AddLabels(labelService);
            
            
            var randomString = RandomGenerator.String();

            var dockerComposeBase64 = dockerComposeEditor.ToBase64();

            dynamic healthCheckOptions = new { checkForDockerHealth = true };
            var recipe = new DeploymentRecipe
            {
                Name = "My Application",
                Application = new() { Id = request.ApplicationId },
                Destination =
                    new DeploymentRecipeDestination { RootDirectory = "/tmp/cloudcrafter/" + randomString },
                DockerComposeOptions =
                    new DeploymentRecipeDockerComposeOptions
                    {
                        Base64DockerCompose = dockerComposeBase64,
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
                            Name  = "Check if network exists",
                            Description = "Check if network exists",
                            Type = DeploymentBuildStepType.DockerValidateNetworksExists,
                            Params = new Dictionary<string, object>()
                            {
                                {
                                    "networks", new List<string>()
                                    {
                                        "cloudcrafter"
                                    }
                                }
                            }
                        },
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
                            Params =
                                new Dictionary<string, object>
                                {
                                    { "path", "nixpacks-node-server" },
                                    { "image", imageRepository },
                                    { "tag", imageTag },
                                    { "disableCache", true },
                                    { "env", new Dictionary<string, object>()
                                    {
                                        {
                                            "BUILD_MOMENT", DateTime.UtcNow.ToString("F")
                                        }
                                    }}
                                }
                        },
                        new()
                        {
                            Name = "Write docker compose file",
                            Description = "Write docker compose file",
                            Type = DeploymentBuildStepType.DockerComposeWriteToFileSystem,
                            Params =
                                new Dictionary<string, object> { { "dockerComposeFile", "docker-compose.yml" } }
                        },
                        new()
                        {
                            Name = "Start docker compose",
                            Description = "Start docker compose",
                            Type = DeploymentBuildStepType.DockerComposeUp,
                            Params =
                                new Dictionary<string, object>
                                {
                                    { "dockerComposeFile", "docker-compose.yml" }, { "storeServiceNames", true }
                                }
                        },
                        new()
                        {
                            Name = "Check if container is healthy",
                            Description = "Check if container is healthy",
                            Type = DeploymentBuildStepType.ContainerHealthCheck,
                            Params = new Dictionary<string, object>
                            {
                                {
                                    "dockerComposeSettings",
                                    new Dictionary<string, object>() { { "fetchServicesFromContext", true } }
                                },
                                {
                                    "services",
                                    new Dictionary<string, object>()
                                    {
                                        {
                                            "frontend",
                                            new Dictionary<string, object>()
                                            {
                                                { "httpMethod", "get" },
                                                { "httpSchema", "http" },
                                                { "httpHost", "localhost" },
                                                { "httpPath", "/" },
                                                { "httpPort", 3000 },
                                                { "expectedResponseCode", 200 },
                                                { "retries", 4 }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };

            return Task.FromResult(recipe);
        }
    }
}
