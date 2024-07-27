using CloudCrafter.Agent.Models.Deployment.Steps.Params.Container;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps.Container;
using FluentAssertions;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps.Container;

public class ContainerHealthCheckHandlerTest : BaseTest
{
    private Mock<IMessagePump> _mockPump;
    private Mock<IDockerHealthCheckHelper> _mockDockerHealthCheckHelper;
    private Mock<IDeploymentLogger> _mockLogger;
    private ContainerHealthCheckHandler _handler;
    private DeploymentContext _context;

    [SetUp]
    public void Setup()
    {
        _mockPump = new Mock<IMessagePump>();
        _mockDockerHealthCheckHelper = new Mock<IDockerHealthCheckHelper>();
        _mockLogger = new Mock<IDeploymentLogger>();
        _mockPump.Setup(p => p.CreateLogger<ContainerHealthCheckHandler>()).Returns(_mockLogger.Object);

        _handler = new ContainerHealthCheckHandler(_mockPump.Object, _mockDockerHealthCheckHelper.Object);
        _context = new DeploymentContext(GetTestRecipe());
    }

    [Test]
    public async Task ExecuteAsync_ShouldCheckHealthOfAllServices_WhenAllAreHealthy()
    {
        // Arrange
        var parameters = new ContainerHealthCheckParams
        {
            Services = new Dictionary<string, ContainerHealthCheckParamsOptions>
            {
                { "service1", new ContainerHealthCheckParamsOptions { CheckInterval = 30 } },
                { "service2", new ContainerHealthCheckParamsOptions { CheckInterval = 60 } }
            }
        };

        _mockDockerHealthCheckHelper.Setup(d => d.IsHealthyAsync(It.IsAny<string>(), It.IsAny<ContainerHealthCheckParamsOptions>()))
            .ReturnsAsync(true);

        // Act
        await _handler.ExecuteAsync(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Running container health check"), Times.Once);
        _mockLogger.Verify(l => l.LogInfo("All provided containers are healthy"), Times.Once);
        _mockDockerHealthCheckHelper.Verify(d => d.IsHealthyAsync("service1", It.IsAny<ContainerHealthCheckParamsOptions>()), Times.Once);
        _mockDockerHealthCheckHelper.Verify(d => d.IsHealthyAsync("service2", It.IsAny<ContainerHealthCheckParamsOptions>()), Times.Once);
    }

    [Test]
    public void ExecuteAsync_ShouldThrowDeploymentException_WhenAnyServiceIsUnhealthy()
    {
        // Arrange
        var parameters = new ContainerHealthCheckParams
        {
            Services = new Dictionary<string, ContainerHealthCheckParamsOptions>
            {
                { "service1", new ContainerHealthCheckParamsOptions { CheckInterval = 30 } },
                { "service2", new ContainerHealthCheckParamsOptions { CheckInterval = 60 } }
            }
        };

        _mockDockerHealthCheckHelper.Setup(d => d.IsHealthyAsync("service1", It.IsAny<ContainerHealthCheckParamsOptions>())).ReturnsAsync(true);
        _mockDockerHealthCheckHelper.Setup(d => d.IsHealthyAsync("service2", It.IsAny<ContainerHealthCheckParamsOptions>())).ReturnsAsync(false);

        // Act & Assert
        _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
            .Should().ThrowAsync<DeploymentException>()
            .WithMessage("Container health checks failed for services: service2");

        _mockLogger.Verify(l => l.LogCritical("The following containers did not become healthy: service2"), Times.Once);
    }

    [Test]
    public void ExecuteAsync_ShouldThrowDeploymentException_WhenNoServicesProvided()
    {
        // Arrange
        var parameters = new ContainerHealthCheckParams
        {
            Services = new Dictionary<string, ContainerHealthCheckParamsOptions>()
        };

        // Act & Assert
        _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
            .Should().ThrowAsync<DeploymentException>()
            .WithMessage("Container health check options not found.");
    }

    [Test]
    public async Task ExecuteAsync_ShouldUseDockerComposeServiceNames_WhenFetchServicesFromContextIsTrue()
    {
        // Arrange
        var parameters = new ContainerHealthCheckParams
        {
            Services = new Dictionary<string, ContainerHealthCheckParamsOptions>
            {
                { "service1", new ContainerHealthCheckParamsOptions { CheckInterval = 30 } },
                { "service2", new ContainerHealthCheckParamsOptions { CheckInterval = 60 } }
            },
            DockerComposeSettings =
                new ContainerHealthCheckParamsOptionsDockerComposeSettings { FetchServicesFromContext = true }
        };

        var dockerComposeServices = new Dictionary<string, string>
        {
            { "service1", "container1" }, { "service2", "container2" }
        };

        _context.SetRecipeResult(RecipeResultKeys.DockerComposeServices, dockerComposeServices);

        _mockDockerHealthCheckHelper.Setup(d => d.IsHealthyAsync(It.IsAny<string>(), It.IsAny<ContainerHealthCheckParamsOptions>()))
            .ReturnsAsync(true);

        // Act
        await _handler.ExecuteAsync(parameters, _context);

        // Assert
        _mockDockerHealthCheckHelper.Verify(d => d.IsHealthyAsync("container1", It.IsAny<ContainerHealthCheckParamsOptions>()), Times.Once);
        _mockDockerHealthCheckHelper.Verify(d => d.IsHealthyAsync("container2", It.IsAny<ContainerHealthCheckParamsOptions>()), Times.Once);
    }

    [Test]
    public async Task DryRun_ShouldLogInfoMessage()
    {
        // Arrange
        var parameters = new ContainerHealthCheckParams();

        // Act
        await _handler.DryRun(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Running container health check in dry run mode"), Times.Once);
    }
}
