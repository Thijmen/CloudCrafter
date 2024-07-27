using System.Text;
using CloudCrafter.Agent.Models.Deployment.Steps.Params.DockerCompose;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps.DockerCompose;
using FluentAssertions;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps.DockerCompose;

public class DockerComposeWriteToFileSystemHandlerTest : BaseTest
{
    private Mock<IMessagePump> _mockPump;
    private Mock<IFileSystemHelper> _mockFileSystemHelper;
    private Mock<IDeploymentLogger> _mockLogger;
    private DockerComposeWriteToFileSystemHandler _handler;
    private DeploymentContext _context;

    [SetUp]
    public void Setup()
    {
        _mockPump = new Mock<IMessagePump>();
        _mockFileSystemHelper = new Mock<IFileSystemHelper>();
        _mockLogger = new Mock<IDeploymentLogger>();
        _mockPump.Setup(p => p.CreateLogger<DockerComposeWriteToFileSystemHandler>()).Returns(_mockLogger.Object);

        _handler = new DockerComposeWriteToFileSystemHandler(_mockPump.Object, _mockFileSystemHelper.Object);
        _context = new DeploymentContext(GetTestRecipe());
    }

    [Test]
    public async Task ExecuteAsync_ShouldWriteDockerComposeFile()
    {
        // Arrange
        var parameters = new DockerComposeWriteToFileSystemParams { DockerComposeFile = "docker-compose.yml" };
        var dockerComposeContent = "version: '3'\nservices:\n  web:\n    image: nginx";
        var base64DockerCompose = Convert.ToBase64String(Encoding.UTF8.GetBytes(dockerComposeContent));
        var dockerComposeDirectory = "/app/docker";

        _context.Recipe.DockerComposeOptions = new()
        {
            Base64DockerCompose = base64DockerCompose, DockerComposeDirectory = dockerComposeDirectory
        };

        var expectedFullPath = $"{dockerComposeDirectory}/{parameters.DockerComposeFile}";

        // Act
        await _handler.ExecuteAsync(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Writing docker compose file to the file system"), Times.Once);
        _mockLogger.Verify(l => l.LogInfo($"Successfully wrote docker compose file to {expectedFullPath}"), Times.Once);
        _mockFileSystemHelper.Verify(f => f.EnsureDirectoryExists(dockerComposeDirectory), Times.Once);
        _mockFileSystemHelper.Verify(f => f.WriteFile(expectedFullPath, dockerComposeContent), Times.Once);
    }

    [Test]
    public void ExecuteAsync_ShouldThrowDeploymentException_WhenDockerComposeOptionsAreNull()
    {
        // Arrange
        var parameters = new DockerComposeWriteToFileSystemParams { DockerComposeFile = "docker-compose.yml" };
        _context.Recipe.DockerComposeOptions = null;

        // Act & Assert
        _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
            .Should().ThrowAsync<DeploymentException>()
            .WithMessage("Docker compose options not found - cannot write docker compose file.");
    }

    [Test]
    public void ExecuteAsync_ShouldThrowDeploymentException_WhenBase64DockerComposeIsEmpty()
    {
        // Arrange
        var parameters = new DockerComposeWriteToFileSystemParams { DockerComposeFile = "docker-compose.yml" };
        _context.Recipe.DockerComposeOptions = new()
        {
            Base64DockerCompose = string.Empty, DockerComposeDirectory = "/app/docker"
        };

        // Act & Assert
        _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
            .Should().ThrowAsync<DeploymentException>()
            .WithMessage("Docker compose options not found - cannot write docker compose file.");
    }

    [Test]
    public void ExecuteAsync_ShouldThrowDeploymentException_WhenDockerComposeDirectoryIsEmpty()
    {
        // Arrange
        var parameters = new DockerComposeWriteToFileSystemParams { DockerComposeFile = "docker-compose.yml" };
        _context.Recipe.DockerComposeOptions = new()
        {
            Base64DockerCompose = "c29tZSBjb250ZW50", DockerComposeDirectory = string.Empty
        };

        // Act & Assert
        _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
            .Should().ThrowAsync<DeploymentException>()
            .WithMessage("Docker compose options not found - cannot write docker compose file.");
    }

    [Test]
    public async Task DryRun_ShouldLogInfoMessage()
    {
        // Arrange
        var parameters = new DockerComposeWriteToFileSystemParams();

        // Act
        await _handler.DryRun(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Writing docker compose file to the file system"), Times.Once);
    }
}
