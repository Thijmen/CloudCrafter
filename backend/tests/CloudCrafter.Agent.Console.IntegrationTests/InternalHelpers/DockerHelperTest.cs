using CloudCrafter.Agent.Runner.Cli.Helpers;

namespace CloudCrafter.Agent.Console.IntegrationTests.InternalHelpers;

public class DockerHelperTest : BaseDockerTest
{
    private DockerHelper _dockerHelper;

    [SetUp]
    public void SetUp()
    {
        _dockerHelper = new DockerHelper(new DockerClientProvider());
    }
    
    [Test]
    public async Task ShouldBeAbleToRunCommandInContainer()
    {
        var nginxContainerId = GetNginxContainerId();

        var list = new List<DockerHelperResponse>();
        await _dockerHelper.RunCommandInContainer(nginxContainerId!, ["whoami"], log =>
        {
            list.Add(log);
        });
        
        list.Should().NotBeEmpty();
        list.Should().HaveCount(1);
        
        list[0].IsStdOut.Should().BeTrue();
        list[0].Response.Should().Be("root\n");
    }
}
