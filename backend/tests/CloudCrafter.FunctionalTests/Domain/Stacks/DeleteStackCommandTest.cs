using CloudCrafter.Core.Commands.Stacks;
using CloudCrafter.Core.Exceptions;
using CloudCrafter.Domain.Entities;
using FluentAssertions;

namespace CloudCrafter.FunctionalTests.Domain.Stacks;

using static Testing;

public class DeleteStackCommandTest : BaseStackTest
{
    private readonly DeleteStackCommand Command = new() { StackId = Guid.NewGuid() };

    [Test]
    public void ShouldThrowExceptionWhenUserIsNotLoggedIn()
    {
        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await SendAsync(Command));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldNotBeAbleToDeleteNonExistingStack(bool isAdmin)
    {
        await RunAsUserRoleAsync(isAdmin);

        Assert.ThrowsAsync<UnauthorizedAccessException>(async () => await SendAsync(Command));
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldNotBeAbleToDeleteExistingTaskBecauseNotEnoughPermissions(
        bool attachToTeam
    )
    {
        var userId = await RunAsDefaultUserAsync();

        var team = await CreateTeam();

        if (attachToTeam)
        {
            await AddToTeam(team, userId);
        }

        var stack = await CreateStack(team.Id);

        Assert.ThrowsAsync<NotEnoughPermissionInTeamException>(
            async () => await SendAsync(Command with { StackId = stack.Id })
        );
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task ShouldBeAbleToDeleteStack(bool isAdmin)
    {
        var userId = await RunAsUserRoleAsync(isAdmin);

        var team = await CreateTeam(!isAdmin ? userId : null);

        var stack = await CreateStack(team.Id);

        await AddStackRelatedEntities(stack);

        (await CountAsync<Stack>()).Should().BeGreaterThan(0);
        (await CountAsync<StackService>()).Should().BeGreaterThan(0);
        (await CountAsync<StackServiceVolume>()).Should().BeGreaterThan(0);
        (await CountAsync<StackEnvironmentVariable>()).Should().BeGreaterThan(0);
        (await CountAsync<StackEnvironmentVariableGroup>()).Should().BeGreaterThan(0);

        await SendAsync(Command with { StackId = stack.Id });

        (await CountAsync<Stack>()).Should().Be(0);
        (await CountAsync<StackService>()).Should().Be(0);
        (await CountAsync<StackServiceVolume>()).Should().Be(0);
        (await CountAsync<StackEnvironmentVariable>()).Should().Be(0);
        (await CountAsync<StackEnvironmentVariableGroup>()).Should().Be(0);
    }
}
