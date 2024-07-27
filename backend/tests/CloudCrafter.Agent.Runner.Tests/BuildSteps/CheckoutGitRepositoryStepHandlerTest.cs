using CloudCrafter.Agent.Models.Deployment.Steps.Params;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps;
using Moq;

namespace CloudCrafter.Agent.Runner.Tests.BuildSteps;

public class CheckoutGitRepositoryStepHandlerTest : BaseTest
{
    private DeploymentContext _context;
    private CheckoutGitRepositoryStepHandler _handler;
    private Mock<ICommandExecutor> _mockExecutor;
    private Mock<IDeploymentLogger> _mockLogger;

    private Mock<IMessagePump> _mockPump;
    private GitCheckoutParams _params;
    private DeploymentRecipe _recipe;

    [SetUp]
    public void SetUp()
    {
        _mockPump = new Mock<IMessagePump>();
        _mockExecutor = new Mock<ICommandExecutor>();
        _mockLogger = new Mock<IDeploymentLogger>();
        _mockPump.Setup(p => p.CreateLogger<CheckoutGitRepositoryStepHandler>()).Returns(_mockLogger.Object);

        _handler = new CheckoutGitRepositoryStepHandler(_mockPump.Object, _mockExecutor.Object);

        _params = new GitCheckoutParams { Repo = "https://github.com/example/repo.git" };
        _recipe = GetTestRecipe();
        _context = new DeploymentContext(_recipe);
    }


    [Test]
    public async Task ExecuteAsync_SuccessfulClone_LogsCorrectly()
    {
        var result = new ExecutorResult { ExitCode = 0 };
        // Arrange
        _mockExecutor.Setup(e => e.ExecuteAsync("git", It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(result);

        var gitDir = _context.GetWorkingDirectory() + "/git";
        // Act
        await _handler.ExecuteAsync(_params, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Start ExecuteAsync"), Times.Once);
        _mockLogger.Verify(l => l.LogInfo($"Cloning repository https://github.com/example/repo.git to {gitDir}"),
            Times.Once);
        _mockExecutor.Verify(
            e => e.ExecuteAsync("git", new[] { "clone", "https://github.com/example/repo.git", gitDir }), Times.Once);
    }

    [Test]
    public void ExecuteAsync_FailedClone_ThrowsDeploymentException()
    {
        var result = new ExecutorResult { ExitCode = 1 };
        // Arrange
        _mockExecutor.Setup(e => e.ExecuteAsync("git", It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(result);

        // Act & Assert
        Assert.ThrowsAsync<DeploymentException>(async () => await _handler.ExecuteAsync(_params, _context));
        _mockLogger.Verify(l => l.LogCritical("Failed to clone git repository"), Times.Once);
    }

    [Test]
    public async Task DryRun_LogsCorrectly()
    {
        // Act
        await _handler.DryRun(_params, _context);

        // Assert
        _mockLogger.Verify(l => l.LogInfo("Checkout Git Repository dryrun"), Times.Once);
    }
}
