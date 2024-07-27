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

public class NixpacksWritePlanToFileSystemHandlerTest : BaseTest
{
    private Mock<IMessagePump> _mockPump;
    private Mock<IFileSystemHelper> _mockFileSystemHelper;
    private Mock<IDeploymentLogger> _mockLogger;
    private NixpacksWritePlanToFileSystemHandler _handler;
    private DeploymentContext _context;

    [SetUp]
    public void Setup()
    {
        _mockPump = new Mock<IMessagePump>();
        _mockFileSystemHelper = new Mock<IFileSystemHelper>();
        _mockLogger = new Mock<IDeploymentLogger>();
        _mockPump.Setup(p => p.CreateLogger<NixpacksWritePlanToFileSystemHandler>()).Returns(_mockLogger.Object);

        _handler = new NixpacksWritePlanToFileSystemHandler(_mockPump.Object, _mockFileSystemHelper.Object);
        _context = new DeploymentContext(GetTestRecipe());
    }

    [Test]
    public async Task ExecuteAsync_ShouldWritePlanToFileSystem()
    {
        // Arrange
        var parameters = new NixpacksWritePlanToFileSystemParams();
        var expectedPlan = "sample build plan";
        var expectedPlanPath = $"{_context.GetWorkingDirectory()}/nixpacks-plan.toml";

        _context.SetRecipeResult(RecipeResultKeys.NixpacksBuildPlan, expectedPlan);

        // Act
        await _handler.ExecuteAsync(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Writing Nixpacks plan to filesystem"), Times.Once);
        _mockLogger.Verify(l => l.LogInfo($"Successfully wrote Nixpacks plan to '{expectedPlanPath}'"), Times.Once);
        _mockFileSystemHelper.Verify(f => f.WriteFile(expectedPlanPath, expectedPlan), Times.Once);

        _context.GetRecipeResult<string>(RecipeResultKeys.NixpacksTomlLocation).Should().Be(expectedPlanPath);
    }

    [Test]
    public void ExecuteAsync_ShouldThrowDeploymentException_WhenPlanIsEmpty()
    {
        // Arrange
        var parameters = new NixpacksWritePlanToFileSystemParams();
    

        _context.SetRecipeResult(RecipeResultKeys.NixpacksBuildPlan, string.Empty);

        // Act & Assert
        _handler.Invoking(h => h.ExecuteAsync(parameters, _context))
            .Should().ThrowAsync<DeploymentException>()
            .WithMessage("Nixpacks plan not found - cannot write to filesystem.");
    }
    

    [Test]
    public async Task DryRun_ShouldLogInfoMessage()
    {
        // Arrange
        var parameters = new NixpacksWritePlanToFileSystemParams();

        // Act
        await _handler.DryRun(parameters, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Writing Nixpacks plan to file system"), Times.Once);
    }
}
