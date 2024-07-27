using CloudCrafter.Agent.Models.Deployment.Steps.Params.Network;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps.Network;
using Docker.DotNet.Models;
using FluentAssertions;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps.Network;

public class NetworkExistsCheckHandlerTest : BaseTest
{
    private Mock<IMessagePump> _mockPump;
    private Mock<IDockerHelper> _mockDockerHelper;
    private Mock<IDeploymentLogger> _mockLogger;
    private NetworkExistsCheckHandler _handler;
    private DeploymentContext _context;

    [SetUp]
    public void Setup()
    {
        _mockPump = new Mock<IMessagePump>();
        _mockDockerHelper = new Mock<IDockerHelper>();
        _mockLogger = new Mock<IDeploymentLogger>();

        _mockPump.Setup(x => x.CreateLogger<NetworkExistsCheckHandler>()).Returns(_mockLogger.Object);

        _context = new DeploymentContext(GetTestRecipe());
        _handler = new NetworkExistsCheckHandler(_mockPump.Object, _mockDockerHelper.Object);
    }

    [Test]
    public async Task ExecuteAsync_AllNetworksExist_ShouldNotThrowException()
    {
        // Arrange
        var parameters = new NetworkExistsCheckParams { Networks = new List<string> { "network1", "network2" } };

        _mockDockerHelper.Setup(x => x.GetNetworks()).ReturnsAsync(new List<NetworkResponse>
        {
            new() { Name = "network1" }, new() { Name = "network2" }, new() { Name = "network3" }
        });

        // Act
        Func<Task> act = async () => await _handler.ExecuteAsync(parameters, _context);

        // Assert
        await act.Should().NotThrowAsync();
        _mockLogger.Verify(x => x.LogInfo("Checking if provided networks exist"), Times.Once);
        _mockLogger.Verify(x => x.LogInfo("All provided networks exist"), Times.Once);
    }

    [Test]
    public async Task ExecuteAsync_SomeNetworksDoNotExist_ShouldThrowDeploymentException()
    {
        // Arrange
        var parameters =
            new NetworkExistsCheckParams { Networks = new List<string> { "network1", "network2", "network4" } };


        _mockDockerHelper.Setup(x => x.GetNetworks()).ReturnsAsync(new List<NetworkResponse>
        {
            new() { Name = "network1" }, new() { Name = "network3" }
        });

        // Act
        Func<Task> act = async () => await _handler.ExecuteAsync(parameters, _context);

        // Assert
        await act.Should().ThrowAsync<DeploymentException>()
            .WithMessage("Networks network2, network4 do not exist");
        _mockLogger.Verify(x => x.LogInfo("Checking if provided networks exist"), Times.Once);
        _mockLogger.Verify(x => x.LogCritical("Networks network2, network4 do not exist"), Times.Once);
    }

    [Test]
    public async Task DryRun_ShouldLogInfoMessage()
    {
        // Arrange
        var parameters = new NetworkExistsCheckParams();


        // Act
        await _handler.DryRun(parameters, _context);

        // Assert
        _mockLogger.Verify(x => x.LogInfo("Checking if provided networks exist in dry run mode"), Times.Once);
    }
}
