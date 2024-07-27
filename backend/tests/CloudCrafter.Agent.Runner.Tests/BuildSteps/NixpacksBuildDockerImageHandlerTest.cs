using CloudCrafter.Agent.Models.Configs;
using CloudCrafter.Agent.Models.Deployment.Steps.Params;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps;
using FluentAssertions;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps;

public class NixpacksBuildDockerImageHandlerTest : BaseTest
{
    private Mock<IMessagePump> _mockPump;
    private Mock<INixpacksHelper> _mockNixpacksHelper;
    private Mock<IDeploymentLogger> _mockLogger;
    private NixpacksBuildDockerImageHandler _handler;
    private DeploymentContext _context;
    private DeploymentRecipe _recipe;

    [SetUp]
    public void Setup()
    {
        _mockPump = new Mock<IMessagePump>();
        _mockNixpacksHelper = new Mock<INixpacksHelper>();
        _mockLogger = new Mock<IDeploymentLogger>();
        _mockPump.Setup(p => p.CreateLogger<NixpacksBuildDockerImageHandler>()).Returns(_mockLogger.Object);

        _handler = new NixpacksBuildDockerImageHandler(_mockPump.Object, _mockNixpacksHelper.Object);
        _recipe = GetTestRecipe();
        _context = new DeploymentContext(_recipe);
    }

    [Test]
    public async Task ExecuteAsync_ShouldBuildDockerImage_WhenValidParameters()
    {
        // Arrange
        var parameters = new NixpacksBuildDockerImageParams
        {
            Path = "testPath", Image = "testImage", Tag = "testTag", DisableCache = false
        };

        _context.SetRecipeResult(RecipeResultKeys.NixpacksTomlLocation, "testPlanPath");

        var workingDirectory = _context.GetWorkingDirectory();
        // Act
        await _handler.ExecuteAsync(parameters, _context);

        // Assert
        _mockNixpacksHelper.Verify(x => x.BuildDockerImage(It.Is<NixpacksBuildDockerImageConfig>(p =>
            p.PlanPath == "testPlanPath" &&
            p.WorkDir == $"{workingDirectory}/git/testPath" &&
            p.ImageName == "testImage:testTag" &&
            p.DisableCache == false
        )), Times.Once);


        _mockLogger.Verify(x => x.LogInfo("Building Docker image via Nixpacks"), Times.Once);
        _mockLogger.Verify(x => x.LogInfo("Successfully built Docker image: testImage:testTag"), Times.Once);
    }

    [Test]
    public void ExecuteAsync_ShouldThrowDeploymentException_WhenNixpacksPlanNotFound()
    {
        // Arrange
        var parameters = new NixpacksBuildDockerImageParams { Path = "testPath", Image = "testImage", Tag = "testTag" };

        _context.SetRecipeResult(RecipeResultKeys.NixpacksTomlLocation, "");

        // Act & Assert
        Func<Task> act = async () => await _handler.ExecuteAsync(parameters, _context);
        act.Should().ThrowAsync<DeploymentException>()
            .WithMessage("Nixpacks plan not found - cannot build Docker image.");
    }

    [Test]
    public async Task DryRun_ShouldLogInfo()
    {
        // Arrange
        var parameters = new NixpacksBuildDockerImageParams();

        // Act
        await _handler.DryRun(parameters, _context);

        // Assert
        _mockLogger.Verify(x => x.LogInfo("Building Docker image via Nixpacks"), Times.Once);
    }
}
