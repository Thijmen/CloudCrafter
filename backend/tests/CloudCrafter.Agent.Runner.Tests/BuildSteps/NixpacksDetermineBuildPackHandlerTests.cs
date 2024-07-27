using CloudCrafter.Agent.Models.Deployment.Steps.Params;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps;
using FluentAssertions;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps;

public class NixpacksDetermineBuildPackHandlerTests : BaseTest
{
    private Mock<IMessagePump> _mockPump;
    private Mock<INixpacksHelper> _mockNixpacksHelper;
    private Mock<IDeploymentLogger> _mockLogger;
    private NixpacksDetermineBuildPackHandler _handler;
    private DeploymentContext _context;

    [SetUp]
    public void Setup()
    {
        _mockPump = new Mock<IMessagePump>();
        _mockNixpacksHelper = new Mock<INixpacksHelper>();
        _mockLogger = new Mock<IDeploymentLogger>();
        _mockPump.Setup(p => p.CreateLogger<NixpacksDetermineBuildPackHandler>()).Returns(_mockLogger.Object);

        _handler = new NixpacksDetermineBuildPackHandler(_mockPump.Object, _mockNixpacksHelper.Object);
        _context = new DeploymentContext(GetTestRecipe());
    }

    [Test]
    public async Task ExecuteAsync_ShouldDetermineBuildPackAndSetRecipeResult()
    {
        // Arrange
        var parameters = new NixpacksDetermineBuildPackParams { Path = "testPath" };
        var expectedFullPath = $"{_context.GetWorkingDirectory()}/git/testPath";
        var expectedBuildPack = "nodejs";

        _mockNixpacksHelper.Setup(n => n.DetermineBuildPackAsync(expectedFullPath))
            .ReturnsAsync(expectedBuildPack);

        // Act
        await _handler.ExecuteAsync(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Starting nixpacks build pack handler"), Times.Once);
        _mockLogger.Verify(l => l.LogInfo($"Determined build pack: '{expectedBuildPack}'"), Times.Once);
        _mockNixpacksHelper.Verify(n => n.DetermineBuildPackAsync(expectedFullPath), Times.Once);

        _context.GetRecipeResult<string>(RecipeResultKeys.NixpacksBuildPack).Should().Be(expectedBuildPack);
    }

    [Test]
    public void ExecuteAsync_ShouldThrowDeploymentException_WhenBuildPackIsEmpty()
    {
        // Arrange
        var parameters = new NixpacksDetermineBuildPackParams { Path = "testPath" };
        var expectedFullPath = $"{_context.GetWorkingDirectory()}/git/testPath";

        _mockNixpacksHelper.Setup(n => n.DetermineBuildPackAsync(expectedFullPath))
            .ReturnsAsync(string.Empty);
    
        // Act & Assert
        _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
            .Should().ThrowAsync<DeploymentException>()
            .WithMessage("Failed to determine build pack");
    }

    [Test]
    public async Task DryRun_ShouldLogInfoMessage()
    {
        // Arrange
        var parameters = new NixpacksDetermineBuildPackParams();

        // Act
        await _handler.DryRun(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Determine nixpacks build pack handler"), Times.Once);
    }
}
