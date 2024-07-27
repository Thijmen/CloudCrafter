using CloudCrafter.Agent.Models.Deployment.Steps.Params;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps;
using FluentAssertions;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps;

public class NixpacksAlterPlanHandlerTest : BaseTest
{
    private DeploymentContext _context;
    private NixpacksAlterPlanHandler _handler;
    private Mock<IDeploymentLogger> _mockLogger;
    private Mock<IMessagePump> _mockPump;
    private NixpacksAlterPlanParams _params;
    private DeploymentRecipe _recipe;

    [SetUp]
    public void SetUp()
    {
        _mockPump = new Mock<IMessagePump>();
        _mockLogger = new Mock<IDeploymentLogger>();
        _mockPump.Setup(p => p.CreateLogger<NixpacksAlterPlanHandler>()).Returns(_mockLogger.Object);

        _handler = new NixpacksAlterPlanHandler(_mockPump.Object);

        _params = new NixpacksAlterPlanParams { Packages = new[] { "package1", "package2" } };
        _recipe = GetTestRecipe();
        _context = new DeploymentContext(_recipe);
    }

    [Test]
    public void ExecuteAsync_WithValidPlan_AltersPlanCorrectly()
    {
        // Arrange
        var initialPlan = """
                          [phases.setup]
                          nixPkgs = ['cowsay']
                          [phases.build]
                          cmds = ['yarn run server:build']
                          """;
        _context.SetRecipeResult(RecipeResultKeys.NixpacksBuildPlan, initialPlan);

        // Act
        _handler.ExecuteAsync(_params, _context);

        // Assert
        var alteredPlan = _context.GetRecipeResult<string>(RecipeResultKeys.NixpacksBuildPlan);
        Assert.That(alteredPlan, Does.Contain("package1"));
        Assert.That(alteredPlan, Does.Contain("package2"));
        Assert.That(alteredPlan, Does.Contain("curl"));
        Assert.That(alteredPlan, Does.Contain("wget"));
        _mockLogger.Verify(l => l.LogInfo("Starting altering nixpacks plan"), Times.Once);
    }

    [Test]
    public void ExecuteAsync_WithNullPlan_ThrowsDeploymentException()
    {
        // Arrange
        _context.SetRecipeResult(RecipeResultKeys.NixpacksBuildPlan, "");

        // Act & Assert
        var exception = Assert.Throws<DeploymentException>(() => _handler.ExecuteAsync(_params, _context));
        exception.Message.Should().Be("Nixpacks plan not found.");
        _mockLogger.Verify(l => l.LogInfo("Starting altering nixpacks plan"), Times.Once);
    }
    
    [Test]
    public async Task DryRun_LogsCorrectly()
    {
        // Act
        await _handler.DryRun(_params, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Alter nixpacks plan handler"), Times.Once);
    }
}
