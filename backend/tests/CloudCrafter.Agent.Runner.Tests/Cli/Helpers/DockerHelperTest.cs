using CloudCrafter.Agent.Models.Docker.Filters;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.DockerCompose.Shared.Labels;
using Docker.DotNet;
using Docker.DotNet.Models;
using FluentAssertions;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.Cli.Helpers;

public class DockerHelperTest
{
    [Test]
    public async Task GetContainerFromFilter_ReturnsFilteredContainers()
    {
        // Arrange
        var mockDockerClient = new Mock<IDockerClient>();
        var mockContainerOperations = new Mock<IContainerOperations>();

        mockDockerClient.Setup(x => x.Containers).Returns(mockContainerOperations.Object);

        var firstApplicationId = Guid.NewGuid();
        var secondApplicationId = Guid.NewGuid();
        var filter = new DockerContainerFilter()
        {
            CloudCrafterApplicationIds = new List<Guid>() { firstApplicationId, secondApplicationId }
        };
        
        var expectedContainers = new List<ContainerListResponse>
        {
            new ContainerListResponse { ID = "container1" },
            new ContainerListResponse { ID = "container2" }
        };

        mockContainerOperations
            .Setup(x => x.ListContainersAsync(
                It.IsAny<ContainersListParameters>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedContainers);

        var provider = new Mock<IDockerClientProvider>();
        provider.Setup(x => x.GetClient())
            .Returns(mockDockerClient.Object);

        var dockerService = new DockerHelper(provider.Object);

        // Act
        var result = await dockerService.GetContainerFromFilter(filter);

        // Assert
        expectedContainers.Count.Should().Be(result.Count);
        mockContainerOperations.Verify(
            x => x.ListContainersAsync(
                It.Is<ContainersListParameters>(p => 
                    p.All == true && 
                    p.Filters.ContainsKey("label") && 
                    ((Dictionary<string, bool>)p.Filters["label"]).Keys.Contains(LabelFactory.GenerateApplicationLabel(firstApplicationId).ToLabelString()) && 
                    ((Dictionary<string, bool>)p.Filters["label"]).Keys.Contains(LabelFactory.GenerateApplicationLabel(secondApplicationId).ToLabelString())),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
