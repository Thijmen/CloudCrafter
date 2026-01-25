using CloudCrafter.Core.Commands.Stacks;
using CloudCrafter.Core.Exceptions;
using CloudCrafter.Domain.Domain.Application.Services;
using CloudCrafter.Domain.Domain.Stack;
using CloudCrafter.Domain.Entities;
using CloudCrafter.Infrastructure.Data.Fakeds;
using FluentAssertions;

namespace CloudCrafter.FunctionalTests.Domain.Stacks;

using static Testing;

public class CreateStackCommandWithDockerComposeTest : BaseTestFixture
{
    private readonly CreateStackCommand _command =
        new()
        {
            Name = "Dummy Stack",
            GitRepository = "https://github.com/cloudcrafter-oss/demo-examples",
            GitBranch = "main",
            PathInGitRepository = "docker-compose-examples/postgresql-server",
            ServerId = Guid.NewGuid(),
            EnvironmentId = Guid.NewGuid(),
            BuildOption = CreateStackBuildOption.DockerCompose,
        };

    [Test]
    public void ShouldThrowExceptionWhenUserIsNotLoggedIn()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await SendAsync(_command));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldThrowExceptionWhenServerIdIsNotFound(bool isAdmin)
    {
        await RunAsUserRoleAsync(isAdmin);
        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await SendAsync(_command)
        );

        exception.Message.Should().Be($"User does not have access to server {_command.ServerId}");

        (await CountAsync<Stack>()).Should().Be(0);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldThrowExceptionWhenEnvironmentIsNotFound(bool isAdmin)
    {
        await RunAsUserRoleAsync(isAdmin);

        var server = FakerInstances.ServerFaker.Generate();
        await AddAsync(server);

        _command.ServerId = server.Id;

        var exception = Assert.ThrowsAsync<UnauthorizedAccessException>(
            async () => await SendAsync(_command)
        );

        var message = isAdmin
            ? $"User does not have access to environment {_command.EnvironmentId}"
            : $"User does not have access to server {_command.ServerId}";

        exception.Message.Should().Be(message);
        (await CountAsync<Stack>()).Should().Be(0);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldBeAbleToCreateStack(bool isAdmin)
    {
        var userId = await RunAsUserRoleAsync(isAdmin);

        var team = await CreateTeam(!isAdmin ? userId : null);

        var server = FakerInstances.ServerFaker.RuleFor(x => x.TeamId, team.Id).Generate();
        await AddAsync(server);

        var project = FakerInstances.ProjectFaker(team.Id).Generate();
        await AddAsync(project);

        var environment = FakerInstances.EnvironmentFaker(project).Generate();
        await AddAsync(environment);

        _command.EnvironmentId = environment.Id;
        _command.ServerId = server.Id;

        (await CountAsync<StackEnvironmentVariableGroup>()).Should().Be(0);

        var result = await SendAsync(_command);

        result.Should().NotBeNull();
        result.Id.Should().NotBe(Guid.Empty);

        (await CountAsync<StackService>()).Should().Be(1);

        var stack = FetchEntity<StackService>(x => x.StackId == result.Id);
        stack.Should().NotBeNull();
        stack!.StackServiceTypeId.Should().Be(StackServiceTypeConstants.DatabasePostgres);
        (await CountAsync<StackEnvironmentVariableGroup>()).Should().Be(2);
        (await CountAsync<StackEnvironmentVariable>()).Should().Be(3);
        (await CountAsync<StackServiceVolume>()).Should().Be(1);

        // Check if the environment variable groups are created
        var groups = FetchEntityList<StackEnvironmentVariableGroup>(x => x.StackId == result.Id);

        groups.Should().HaveCount(2);

        var appSettings = groups.FirstOrDefault(x => x.Name == "Application Settings");
        appSettings.Should().NotBeNull();
        appSettings!.StackId.Should().Be(result.Id);
        appSettings.Description.Should().Be("Basic application environment variables");

        var connectionStrings = groups.FirstOrDefault(x => x.Name == "Database Settings");
        connectionStrings.Should().NotBeNull();
        connectionStrings!.StackId.Should().Be(result.Id);
        connectionStrings.Description.Should().Be("Database connection environment variables");
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldNotBeAbleToCreateStackBecauseUserIsNotCorrectTeamMember(
        bool attachToTeam
    )
    {
        var userId = await RunAsDefaultUserAsync();

        var team = await CreateTeam();

        if (attachToTeam)
        {
            await AddToTeam(team, userId);
        }

        var server = FakerInstances.ServerFaker.RuleFor(x => x.TeamId, team.Id).Generate();
        await AddAsync(server);

        var project = FakerInstances.ProjectFaker(team.Id).Generate();
        await AddAsync(project);

        var environment = FakerInstances.EnvironmentFaker(project).Generate();
        await AddAsync(environment);

        _command.EnvironmentId = environment.Id;
        _command.ServerId = server.Id;

        (await CountAsync<StackEnvironmentVariableGroup>()).Should().Be(0);

        if (!attachToTeam)
        {
            Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await SendAsync(_command));
        }
        else
        {
            Assert.ThrowsAsync<NotEnoughPermissionInTeamException>(
                async () => await SendAsync(_command)
            );
        }
    }
}
