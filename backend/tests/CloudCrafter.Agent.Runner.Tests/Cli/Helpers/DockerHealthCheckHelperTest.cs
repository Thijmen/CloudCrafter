using CloudCrafter.Agent.Models.Deployment.Steps.Params.Container;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.Exceptions;
using Docker.DotNet.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.Cli.Helpers;

public class DockerHealthCheckHelperTest
{
    private Mock<IDockerHelper> _mockDockerHelper;
    private Mock<ILogger<DockerHealthCheckHelper>> _mockLogger;
    private DockerHealthCheckHelper _dockerHealthCheckHelper;

    [SetUp]
    public void Setup()
    {
        _mockDockerHelper = new Mock<IDockerHelper>();
        _mockLogger = new Mock<ILogger<DockerHealthCheckHelper>>();
        _dockerHealthCheckHelper = new DockerHealthCheckHelper(_mockDockerHelper.Object, _mockLogger.Object);
    }

    [Test]
    public async Task IsHealthyAsync_WhenContainerIsHealthy_ReturnsTrue()
    {
        // Arrange
        var containerId = "test-container";
        var options = new ContainerHealthCheckParamsOptions
        {
            CheckForDockerHealth = true,
            HttpSchema = "http",
            HttpHost = "localhost",
            HttpPort = 8080,
            HttpPath = "/health",
            ExpectedResponseCode = 200,
            Retries = 1
        };

        _mockDockerHelper.Setup(x => x.GetDockerContainer(containerId))
            .ReturnsAsync(new ContainerInspectResponse
            {
                State = new ContainerState { Health = new () { Status = "healthy" } }
            });

        _mockDockerHelper.Setup(x => x.RunCommandInContainer(containerId, It.IsAny<IList<string>>(), null))
            .ReturnsAsync(
                new() { ExitCode = 0, StdOut = "{\"response_code\":\"200\",\"response_body\":\"OK\"}", StdErr = "" },
                TimeSpan.FromMicroseconds(100));

        // Act
        var result = await _dockerHealthCheckHelper.IsHealthyAsync(containerId, options);

        // Assert
        result.Should().BeTrue();
    }

    [Test]
    public async Task IsHealthyAsync_WhenContainerIsUnhealthy_ReturnsFalse()
    {
        // Arrange
        var containerId = "test-container";
        var options = new ContainerHealthCheckParamsOptions { CheckForDockerHealth = true, Retries = 1 };

        _mockDockerHelper.Setup(x => x.GetDockerContainer(containerId))
            .ReturnsAsync(new ContainerInspectResponse
            {
                State = new ContainerState { Health = new () { Status = "unhealthy" } }
            });

        // Act
        var result = await _dockerHealthCheckHelper.IsHealthyAsync(containerId, options);

        // Assert
        result.Should().BeFalse();
    }

    [Test]
    public void IsHealthyAsync_WhenContainerHasNoHealthCheck_ThrowsException()
    {
        // Arrange
        var containerId = "test-container";
        var options = new ContainerHealthCheckParamsOptions { CheckForDockerHealth = true, Retries = 1 };

        _mockDockerHelper.Setup(x => x.GetDockerContainer(containerId))
            .ReturnsAsync(new ContainerInspectResponse { State = new ContainerState { Health = null } });

        // Act & Assert
        Func<Task> act = async () => await _dockerHealthCheckHelper.IsHealthyAsync(containerId, options);
        act.Should().ThrowAsync<AgentDockerException>()
            .WithMessage(
                "Provided container does not have health check enabled but check options has CheckForDockerHealth = true.");
    }

    [Test]
    public async Task IsHealthyAsync_WhenHealthCheckResponseCodeDoesNotMatch_ReturnsFalse()
    {
        // Arrange
        var containerId = "test-container";
        var options = new ContainerHealthCheckParamsOptions
        {
            CheckForDockerHealth = false,
            HttpSchema = "http",
            HttpHost = "localhost",
            HttpPort = 8080,
            HttpPath = "/health",
            ExpectedResponseCode = 200,
            Retries = 1
        };

        _mockDockerHelper.Setup(x => x.RunCommandInContainer(containerId, It.IsAny<IList<string>>(), null))
            .ReturnsAsync(new DockerHelperResponseResult()
            {
                ExitCode = 0, StdOut = "{\"response_code\":\"500\",\"response_body\":\"Internal Server Error\"}", StdErr = ""
            });

        // Act
        var result = await _dockerHealthCheckHelper.IsHealthyAsync(containerId, options);

        // Assert
        result.Should().BeFalse();
    }
}
