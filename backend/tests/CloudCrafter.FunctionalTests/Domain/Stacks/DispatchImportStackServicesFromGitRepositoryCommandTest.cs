using CloudCrafter.Core.Commands.Stacks;
using CloudCrafter.Core.Exceptions;
using CloudCrafter.Domain.Domain.Application.Services;
using CloudCrafter.Domain.Entities;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CloudCrafter.FunctionalTests.Domain.Stacks;

using static Testing;

public class DispatchImportStackServicesFromGitRepositoryCommandTest : BaseStackTest
{
    private readonly DispatchImportStackServicesFromGitRepositoryCommand Command =
        new() { StackId = Guid.NewGuid() };

    [Test]
    public void ShouldThrowExceptionWhenUserIsNotLoggedIn()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await SendAsync(Command));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldNotBeAbleToDispatchWhenStackDoesNotExists(bool isAdmin)
    {
        await RunAsUserRoleAsync(isAdmin);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await SendAsync(Command));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldNotBeAbleToDispatchBecauseUserHasNotRightPermissions(bool attachToTeam)
    {
        var userId = await RunAsDefaultUserAsync();

        var team = await CreateTeam();

        if (attachToTeam)
        {
            await AddToTeam(team, userId);
        }

        var stack = await CreateStack(team.Id, DemoRepo, DemoBranch, DemoPostgresPath);

        Assert.ThrowsAsync<NotEnoughPermissionInTeamException>(
            async () => await SendAsync(Command with { StackId = stack.Id })
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldNotBeAbleToDispatchBecauseStackIsNotDockerComposeStack(bool isAdmin)
    {
        var userId = await RunAsUserRoleAsync(isAdmin);

        var team = await CreateTeam(!isAdmin ? userId : null);

        var stack = await CreateStack(team.Id, DemoRepo, DemoBranch, DemoPostgresPath);
        (await CountAsync<StackService>()).Should().Be(0);
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await SendAsync(Command with { StackId = stack.Id })
        );

        (await CountAsync<StackService>()).Should().Be(0);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldBeAbleToDispatchCommand(bool isAdmin)
    {
        var userId = await RunAsUserRoleAsync(isAdmin);

        var team = await CreateTeam(!isAdmin ? userId : null);

        var stack = await CreateStack(
            team.Id,
            DemoRepo,
            DemoBranch,
            DemoPostgresPath,
            StackBuildPack.DockerCompose
        );

        (await CountAsync<StackService>()).Should().Be(0);
        await SendAsync(Command with { StackId = stack.Id });
        (await CountAsync<StackService>()).Should().Be(1);

        var stackFromDb = FetchEntity<Stack>(
            s => s.Id == stack.Id,
            inc =>
                inc.Include(s => s.DockerComposeData)
                    .Include(x => x.EnvironmentVariables)
                    .Include(s => s.Services)
                    .ThenInclude(x => x.Volumes)
        );

        stackFromDb.Should().NotBeNull();
        stackFromDb!.DockerComposeData.Should().NotBeNull();
        stackFromDb.BuildPack.Should().Be(StackBuildPack.DockerCompose);
        stackFromDb.DockerComposeData.DockerComposeFile.Should().NotBeNull();
        stackFromDb
            .DockerComposeData.DockerComposeFile.Should()
            .ContainAll("image: postgres:16-alpine", "cloudcrafter.service.type=database");

        stackFromDb.Services.Should().HaveCount(1);

        var service = stackFromDb.Services[0];
        service.StackServiceTypeId.Should().Be(StackServiceTypeConstants.DatabasePostgres);

        // Validate env vars
        // See https://github.com/cloudcrafter-oss/demo-examples/edit/main/docker-compose-examples/postgresql-server/docker-compose.yml
        var envVars = stackFromDb.EnvironmentVariables;
        envVars.Should().HaveCount(3);

        var userEnvVar = envVars.FirstOrDefault(x => x.Key == "POSTGRES_USER");
        userEnvVar.Should().NotBeNull();
        userEnvVar!.Value.Should().Be("cloudcrafter");
        userEnvVar.IsSecret.Should().BeFalse();

        var passwordEnvVar = envVars.FirstOrDefault(x => x.Key == "POSTGRES_PASSWORD");
        passwordEnvVar.Should().NotBeNull();
        passwordEnvVar!.Value.Should().Be("password");
        passwordEnvVar.IsSecret.Should().BeFalse();

        var dbNameEnvVar = envVars.FirstOrDefault(x => x.Key == "POSTGRES_DB");
        dbNameEnvVar.Should().NotBeNull();
        dbNameEnvVar!.Value.Should().Be("demo");
        dbNameEnvVar.IsSecret.Should().BeFalse();

        // Validate volumes
        var volumes = stackFromDb.Services[0].Volumes;
        volumes.Should().HaveCount(1);

        var volume = volumes.FirstOrDefault(x => x.SourcePath == "pgdata");
        volume.Should().NotBeNull();

        volume!.Name.Should().Be("/var/lib/postgresql/data");
        volume.SourcePath.Should().Be("pgdata");
        volume.DestinationPath.Should().Be("/var/lib/postgresql/data");
        volume.Type.Should().Be(StackServiceVolumeType.DockerVolume);
    }
}
