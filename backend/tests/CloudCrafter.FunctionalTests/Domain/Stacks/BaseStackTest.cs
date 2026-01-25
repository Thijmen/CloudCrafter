using CloudCrafter.Domain.Entities;
using CloudCrafter.Infrastructure.Data.Fakeds;
using FluentAssertions;

namespace CloudCrafter.FunctionalTests.Domain.Stacks;

using static Testing;

public abstract class BaseStackTest : BaseTestFixture
{
    public const string DemoRepo = "https://github.com/cloudcrafter-oss/demo-examples";
    public const string DemoBranch = "main";
    public const string DemoPostgresPath = "docker-compose-examples/postgresql-server";

    public async Task<Stack> CreateStack(
        Guid teamId,
        string repository = DemoRepo,
        string branch = DemoBranch,
        string path = DemoPostgresPath,
        StackBuildPack pack = StackBuildPack.Nixpacks
    )
    {
        var project = FakerInstances.ProjectFaker(teamId).Generate();
        await AddAsync(project);

        var environment = FakerInstances.EnvironmentFaker(project).Generate();
        await AddAsync(environment);

        var stack = FakerInstances
            .StackFaker(
                environment.Id,
                new FakerInstances.PublicGitSettings(repository, branch, path)
            )
            .RuleFor(x => x.BuildPack, pack)
            .Generate();
        await AddAsync(stack);

        return stack;
    }

    public async Task AddStackRelatedEntities(Stack stack)
    {
        var stackServices = FakerInstances.StackServiceFaker(stack).Generate(3);
        foreach (var service in stackServices)
        {
            await AddAsync(service);

            var volumes = FakerInstances.StackServiceVolumeFaker(service.Id).Generate(3);

            foreach (var volume in volumes)
            {
                await AddAsync(volume);
            }
        }

        (await CountAsync<StackServiceVolume>()).Should().BeGreaterOrEqualTo(9);

        (await CountAsync<StackService>()).Should().BeGreaterOrEqualTo(3);

        var stackVariables = FakerInstances.StackEnvironmentVariableFaker(stack).Generate(3);
        foreach (var variable in stackVariables)
        {
            await AddAsync(variable);
        }

        (await CountAsync<StackEnvironmentVariable>()).Should().BeGreaterOrEqualTo(3);

        var groupFaker = FakerInstances.StackEnvironmentVariableGroupFaker(stack).Generate(3);
        foreach (var group in groupFaker)
        {
            await AddAsync(group);
        }

        (await CountAsync<StackEnvironmentVariableGroup>()).Should().BeGreaterOrEqualTo(3);
    }
}
