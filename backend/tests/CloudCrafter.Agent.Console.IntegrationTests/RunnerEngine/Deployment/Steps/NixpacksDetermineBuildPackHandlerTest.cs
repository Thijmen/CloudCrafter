using CloudCrafter.Agent.Models.Deployment.Steps.Params;
using CloudCrafter.Agent.Models.Exceptions;
using CloudCrafter.Agent.Models.Runner;
using CloudCrafter.Agent.Runner.Cli;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment.Steps;
using Microsoft.Extensions.DependencyInjection;

namespace CloudCrafter.Agent.Console.IntegrationTests.RunnerEngine.Deployment.Steps;

public class NixpacksDetermineBuildPackHandlerTest : HandlerBaseCase
{
    private NixpacksDetermineBuildPackHandler _handler;
    private CheckoutGitRepositoryStepHandler _checkoutHandler;

    [SetUp]
    public void Setup()
    {
        var builder = Program.CreateHostBuilder([]);
        var host = builder.Build();

        var pump = host.Services.GetRequiredService<IMessagePump>();
        var helper = host.Services.GetRequiredService<INixpacksHelper>();

        var executor = host.Services.GetRequiredService<ICommandExecutor>();
        _handler = new NixpacksDetermineBuildPackHandler(pump, helper);
        _checkoutHandler = new CheckoutGitRepositoryStepHandler(pump, executor);
    }

    [Test]
    public void ShouldNotBeAbleToDetermineNixpacksPackBecauseTheGitRepoIsNotCloned()
    {
        var parameters = new NixpacksDetermineBuildPackParams() { Path = "nixpacks-node-server" };

        var context = GetContext();

        var exception = Assert.ThrowsAsync<DeploymentException>(() => _handler.ExecuteAsync(parameters, context));

        exception.Message.Should()
            .Be(
                $"Could not determine build pack, directory: {context.GetWorkingDirectory()}/git/nixpacks-node-server");
    }

    [Test]
    public async Task ShouldBeAbleToDetermineNixpacksPack()
    {
        var checkoutParams = new GitCheckoutParams() { Repo = "https://github.com/cloudcrafter-oss/demo-examples.git" };
        
        var context = GetContext();

        await _checkoutHandler.ExecuteAsync(checkoutParams, context);
        var parameters = new NixpacksDetermineBuildPackParams() { Path = "nixpacks-node-server" };

        await _handler.ExecuteAsync(parameters, context);
        
        var pack = context.GetRecipeResult<string>(RecipeResultKeys.NixpacksBuildPack);

        pack.Should().Be("node");
    }
}
