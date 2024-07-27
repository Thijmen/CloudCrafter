using CliWrap;
using CloudCrafter.Agent.Models.Deployment.Steps.Params.DockerCompose;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps.DockerCompose;
using FluentAssertions;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps.DockerCompose;

public class DockerComposeUpHandlerTest : BaseTest
{
      private Mock<IMessagePump> _mockPump;
        private Mock<IDockerComposeHelper> _mockDockerComposeHelper;
        private Mock<IDeploymentLogger> _mockLogger;
        private DockerComposeUpHandler _handler;
        private DeploymentContext _context;

        [SetUp]
        public void Setup()
        {
            _mockPump = new Mock<IMessagePump>();
            _mockDockerComposeHelper = new Mock<IDockerComposeHelper>();
            _mockLogger = new Mock<IDeploymentLogger>();
            _mockPump.Setup(p => p.CreateLogger<DockerComposeUpHandler>()).Returns(_mockLogger.Object);

            _handler = new DockerComposeUpHandler(_mockPump.Object, _mockDockerComposeHelper.Object);
            _context = new DeploymentContext(GetTestRecipe());
        }

        [Test]
        public async Task ExecuteAsync_ShouldRunDockerComposeUp_Successfully()
        {
            // Arrange
            var parameters = new DockerComposeUpParams { DockerComposeFile = "docker-compose.yml", StoreServiceNames = true };
            var dockerComposeDirectory = "/app/docker";
            _context.Recipe.DockerComposeOptions = new()
            {
                DockerComposeDirectory = dockerComposeDirectory
            };

            var expectedDockerComposeFile = $"{dockerComposeDirectory}/{parameters.DockerComposeFile}";
            var expectedServices = new Dictionary<string, string>() { { "service-one", "containerid" } };

            _mockDockerComposeHelper.Setup(d => d.UpAsync(expectedDockerComposeFile, null))
                .ReturnsAsync(new ExecutorResult()
                {
                    IsSuccess = true
                });
            _mockDockerComposeHelper.Setup(d => d.GetDockerComposeServices(expectedDockerComposeFile))
                .ReturnsAsync(expectedServices);

            // Act
            await _handler.ExecuteAsync(parameters, _context);

            // Assert
            _mockLogger.Verify(l => l.LogInfo("Running docker compose up"), Times.Once);
            _mockDockerComposeHelper.Verify(d => d.UpAsync(expectedDockerComposeFile, null), Times.Once);
            _mockDockerComposeHelper.Verify(d => d.GetDockerComposeServices(expectedDockerComposeFile), Times.Once);
            _context.GetRecipeResult<Dictionary<string, string>>(RecipeResultKeys.DockerComposeServices).Should().BeEquivalentTo(expectedServices);
        }

        [Test]
        public void ExecuteAsync_ShouldThrowDeploymentException_WhenDockerComposeOptionsAreNull()
        {
            // Arrange
            var parameters = new DockerComposeUpParams { DockerComposeFile = "docker-compose.yml" };
            _context.Recipe.DockerComposeOptions = null;

            // Act & Assert
            _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
                .Should().ThrowAsync<DeploymentException>()
                .WithMessage("Docker compose options not found - cannot write docker compose file.");
        }

        [Test]
        public void ExecuteAsync_ShouldThrowDeploymentException_WhenDockerComposeDirectoryIsEmpty()
        {
            // Arrange
            var parameters = new DockerComposeUpParams { DockerComposeFile = "docker-compose.yml" };
            _context.Recipe.DockerComposeOptions = new ()
            {
                DockerComposeDirectory = string.Empty
            };

            // Act & Assert
            _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
                .Should().ThrowAsync<DeploymentException>()
                .WithMessage("Docker compose options not found - cannot write docker compose file.");
        }

        [Test]
        public void ExecuteAsync_ShouldThrowDeploymentException_WhenDockerComposeUpFails()
        {
            // Arrange
            var parameters = new DockerComposeUpParams { DockerComposeFile = "docker-compose.yml" };
            var dockerComposeDirectory = "/app/docker";
            _context.Recipe.DockerComposeOptions = new()
            {
                DockerComposeDirectory = dockerComposeDirectory
            };

            var expectedDockerComposeFile = $"{dockerComposeDirectory}/{parameters.DockerComposeFile}";

            _mockDockerComposeHelper.Setup(d => d.UpAsync(expectedDockerComposeFile, null))
                .ReturnsAsync(new ExecutorResult { IsSuccess = false });

            // Act & Assert
            _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
                .Should().ThrowAsync<DeploymentException>()
                .WithMessage("Failed to run docker compose up, see logs for more information.");
        }

        [Test]
        public async Task ExecuteAsync_ShouldNotStoreServiceNames_WhenParameterIsFalse()
        {
            // Arrange
            var parameters = new DockerComposeUpParams { DockerComposeFile = "docker-compose.yml", StoreServiceNames = false };
            var dockerComposeDirectory = "/app/docker";
            _context.Recipe.DockerComposeOptions = new()
            {
                DockerComposeDirectory = dockerComposeDirectory
            };

            var expectedDockerComposeFile = $"{dockerComposeDirectory}/{parameters.DockerComposeFile}";

            _mockDockerComposeHelper.Setup(d => d.UpAsync(expectedDockerComposeFile, null))
                .ReturnsAsync(new ExecutorResult { IsSuccess = true });

            // Act
            await _handler.ExecuteAsync(parameters, _context);

            // Assert
            _mockDockerComposeHelper.Verify(d => d.GetDockerComposeServices(It.IsAny<string>()), Times.Never);
            var exception = Assert.Throws<KeyNotFoundException>(() =>
                _context.GetRecipeResult<List<string>>(RecipeResultKeys.DockerComposeServices));
            
            exception.Message.Should().Be("Recipe result with key 'DockerComposeServices' not found");
        }

        [Test]
        public async Task DryRun_ShouldLogInfoMessage()
        {
            // Arrange
            var parameters = new DockerComposeUpParams();

            // Act
            await _handler.DryRun(parameters, _context);

            // Assert
            _mockLogger.Verify(l => l.LogInfo("Running docker compose up in dry run mode"), Times.Once);
        }
}
