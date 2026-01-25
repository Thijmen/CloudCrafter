using CloudCrafter.Core.Commands.Utils;
using FluentAssertions;

namespace CloudCrafter.FunctionalTests.Domain.Utils;

using static Testing;

public class CheckValidGitRepoCommandTest : BaseTestFixture
{
    [TestCase("https://github.com/cloudcrafter-oss/CloudCrafter", "main", "", true)]
    [TestCase("https://gitlab.com/gitlab-org/gitlab", "master", "", true)]
    [TestCase("https://github.com/cloudcrafter-oss/CloudCrafter", "main", "", false)]
    [TestCase("https://gitlab.com/gitlab-org/gitlab", "master", "", false)]
    [TestCase("https://github.com/cloudcrafter-oss/CloudCrafter", "beta", "frontend", true)]
    [TestCase("https://github.com/cloudcrafter-oss/CloudCrafter", "main", "frontend", false)]
    public async Task ShouldBeAValidGitRepo(string repo, string branch, string path, bool isAdmin)
    {
        await RunAsUserRoleAsync(isAdmin);
        var command = new CheckValidGitRepoCommand(repo, branch, path);

        var result = await SendAsync(command);

        result.Should().NotBeNull();
        result.IsValid.Should().BeTrue();
    }

    [TestCase("https://github.com/cloudcrafter-oss/CloudCrafter-not-existing", "main", "", true)]
    [TestCase("https://www.google.nl", "main", "", true)]
    [TestCase("https://github.com/cloudcrafter-oss/CloudCrafter-not-existing", "main", "", false)]
    [TestCase("https://www.google.nl", "main", "", false)]
    [TestCase(
        "https://github.com/cloudcrafter-oss/CloudCrafter",
        "beta",
        "non-existing-path",
        true
    )]
    [TestCase(
        "https://github.com/cloudcrafter-oss/CloudCrafter",
        "main",
        "non-existing-path",
        true
    )]
    [TestCase(
        "https://github.com/cloudcrafter-oss/CloudCrafter",
        "non-existing-branch",
        "frontend",
        false
    )]
    public async Task ShouldBeInvalidGitRepo(string repo, string branch, string path, bool isAdmin)
    {
        await RunAsUserRoleAsync(isAdmin);
        var command = new CheckValidGitRepoCommand(repo, branch, path);

        var result = await SendAsync(command);

        result.Should().NotBeNull();
        result.IsValid.Should().BeFalse();
    }
}
