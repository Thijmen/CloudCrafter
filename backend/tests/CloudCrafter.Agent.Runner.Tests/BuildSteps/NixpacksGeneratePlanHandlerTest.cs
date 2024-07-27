using CloudCrafter.Agent.Models.Deployment.Steps.Params;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps;
using FluentAssertions;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps;

public class NixpacksGeneratePlanHandlerTest : BaseTest
{
    private Mock<IMessagePump> _mockPump;
    private Mock<INixpacksHelper> _mockNixpacksHelper;
    private Mock<IDeploymentLogger> _mockLogger;
    private NixpacksGeneratePlanHandler _handler;
    private DeploymentContext _context;

    [SetUp]
    public void Setup()
    {
        _mockPump = new Mock<IMessagePump>();
        _mockNixpacksHelper = new Mock<INixpacksHelper>();
        _mockLogger = new Mock<IDeploymentLogger>();
        _mockPump.Setup(p => p.CreateLogger<NixpacksGeneratePlanHandler>()).Returns(_mockLogger.Object);

        _handler = new NixpacksGeneratePlanHandler(_mockPump.Object, _mockNixpacksHelper.Object);
        _context = new DeploymentContext(GetTestRecipe());
    }

    [Test]
    public async Task ExecuteAsync_ShouldGeneratePlanAndSetRecipeResult()
    {
        // Arrange
        var parameters = new NixpacksGeneratePlanParams { Path = "testPath" };
        var expectedFullPath = $"{_context.GetWorkingDirectory()}/git/testPath";
        var expectedPlan = "sample build plan";

        _mockNixpacksHelper.Setup(n => n.GetBuildPlanAsync(expectedFullPath, parameters))
            .ReturnsAsync(expectedPlan);

        // Act
        await _handler.ExecuteAsync(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Starting nixpacks plan handler"), Times.Once);
        _mockLogger.Verify(l => l.LogInfo("Saved nixpacks plan to to context."), Times.Once);
        _mockNixpacksHelper.Verify(n => n.GetBuildPlanAsync(expectedFullPath, parameters), Times.Once);

        _context.GetRecipeResult<string>(RecipeResultKeys.NixpacksBuildPlan).Should().Be(expectedPlan);
    }

    [Test]
    public async Task ExecuteAsync_ShouldHandleEmptyPlan()
    {
        // Arrange
        var parameters = new NixpacksGeneratePlanParams { Path = "testPath" };
        var expectedFullPath = $"{_context.GetWorkingDirectory()}/git/testPath";

        _mockNixpacksHelper.Setup(n => n.GetBuildPlanAsync(expectedFullPath, parameters))
            .ReturnsAsync(string.Empty);

        // Act
        await _handler.ExecuteAsync(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Starting nixpacks plan handler"), Times.Once);
        _mockLogger.Verify(l => l.LogInfo("Saved nixpacks plan to to context."), Times.Once);
        _mockNixpacksHelper.Verify(n => n.GetBuildPlanAsync(expectedFullPath, parameters), Times.Once);

        _context.GetRecipeResult<string>(RecipeResultKeys.NixpacksBuildPlan).Should().BeEmpty();
    }

    [Test]
    public async Task DryRun_ShouldLogInfoMessage()
    {
        // Arrange
        var parameters = new NixpacksGeneratePlanParams();

        // Act
        await _handler.DryRun(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Generate nixpacks plan handler"), Times.Once);
    }
}
