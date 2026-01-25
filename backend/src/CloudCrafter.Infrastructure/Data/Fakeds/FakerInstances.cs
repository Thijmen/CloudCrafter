using Bogus;
using CloudCrafter.Domain.Domain.Application.Services;
using CloudCrafter.Domain.Entities;
using CloudCrafter.Domain.Entities.Jobs;
using Environment = CloudCrafter.Domain.Entities.Environment;

namespace CloudCrafter.Infrastructure.Data.Fakeds;

public static class FakerInstances
{
    public static Faker<User> UserFaker =>
        new Faker<User>()
            .RuleFor(u => u.Email, f => f.Internet.Email())
            .RuleFor(u => u.UserName, f => f.Internet.UserName())
            .RuleFor(u => u.PasswordHash, f => f.Internet.Password())
            .RuleFor(u => u.FullName, f => f.Person.FullName);

    public static Faker<Server> ServerFaker =>
        new Faker<Server>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.SshPort, f => f.Random.Number(22, 2000))
            .RuleFor(x => x.Name, f => $"Server {f.Person.FirstName}")
            .RuleFor(x => x.IpAddress, f => f.Internet.Ip())
            .RuleFor(x => x.DockerDataDirectoryMount, "/data/random/dir")
            .RuleFor(x => x.Stacks, [])
            .RuleFor(x => x.DockerNetwork, f => f.Internet.DomainWord())
            .RuleFor(x => x.TeamId, f => null)
            .RuleFor(x => x.Team, f => null)
            .RuleFor(x => x.CreatedBy, f => null)
            .RuleFor(x => x.LastModifiedBy, f => null)
            .RuleFor(x => x.PingHealthData, new ServerPingData())
            .RuleFor(x => x.AgentSecretKey, f => f.Internet.Password())
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);

    public static Faker<EntityStackServiceHealthStatus> EntityHealthStatusFaker =>
        new Faker<EntityStackServiceHealthStatus>()
            .StrictMode(true)
            .RuleFor(x => x.Value, f => f.PickRandom<EntityHealthStatusValue>())
            .RuleFor(x => x.IsRunning, false)
            .RuleFor(x => x.StatusAt, DateTime.UtcNow);

    public static StackServiceType StackServiceAppTypeType =>
        new() { Id = StackServiceTypeConstants.App, Type = nameof(StackServiceTypeConstants.App) };

    public static Faker<GithubProvider> GithubProviderFaker(Guid? teamId = null)
    {
        var githubProviderId = Guid.NewGuid();
        var provider = SourceProviderFaker(githubProviderId)
            .RuleFor(x => x.TeamId, f => teamId)
            .Generate();

        return new Faker<GithubProvider>()
            .StrictMode(true)
            .RuleFor(x => x.Id, githubProviderId)
            .RuleFor(x => x.AppName, f => f.Person.FullName)
            .RuleFor(x => x.IsValid, f => null)
            .RuleFor(x => x.AppId, f => f.Random.Long())
            .RuleFor(x => x.AppClientId, f => f.Random.Guid().ToString())
            .RuleFor(x => x.AppClientSecret, f => f.Random.Guid().ToString())
            .RuleFor(x => x.AppWebhookSecret, f => f.Random.Guid().ToString())
            .RuleFor(x => x.AppPrivateKey, f => f.Random.Guid().ToString())
            .RuleFor(x => x.InstallationId, f => null)
            .RuleFor(x => x.AppUrl, f => f.Internet.Url())
            .RuleFor(x => x.SourceProvider, f => provider)
            .RuleFor(x => x.SourceProviderId, f => provider.Id)
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);
    }

    public static Faker<Project> ProjectFaker(Guid teamId)
    {
        return new Faker<Project>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.Name, f => $"Project {f.Person.FirstName}")
            .RuleFor(x => x.Description, f => f.Lorem.Sentence())
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.TeamId, teamId)
            .RuleFor(x => x.Team, (Team?)null)
            .RuleFor(x => x.Environments, new List<Environment>())
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);
    }

    public static Faker<StackEnvironmentVariable> StackEnvironmentVariableFaker(Stack stack)
    {
        return new Faker<StackEnvironmentVariable>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.Key, f => f.Lorem.Word())
            .RuleFor(x => x.Value, f => f.Lorem.Word())
            .RuleFor(x => x.StackId, stack.Id)
            .RuleFor(x => x.Group, f => null)
            .RuleFor(x => x.GroupId, f => null)
            .RuleFor(x => x.IsSecret, f => false)
            // ReSharper disable once RedundantCast
            .RuleFor(x => x.Stack, f => (Stack?)null)
            .RuleFor(x => x.Type, f => f.PickRandom<EnvironmentVariableType>())
            .RuleFor(x => x.CreatedBy, f => null)
            .RuleFor(x => x.LastModifiedBy, f => null)
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);
    }

    public static Faker<StackEnvironmentVariableGroup> StackEnvironmentVariableGroupFaker(
        Stack stack
    )
    {
        return new Faker<StackEnvironmentVariableGroup>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.CreatedBy, f => null)
            .RuleFor(x => x.Name, f => f.Name.JobTitle())
            .RuleFor(x => x.Description, f => f.Lorem.Sentence())
            .RuleFor(x => x.LastModifiedBy, f => null)
            .RuleFor(x => x.StackId, stack.Id)
            // ReSharper disable once RedundantCast
            .RuleFor(x => x.Stack, (Stack?)null)
            .RuleFor(x => x.EnvironmentVariables, f => new List<StackEnvironmentVariable>())
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);
    }

    public static Faker<SourceProvider> SourceProviderFaker(Guid? githubProviderId = null)
    {
        return new Faker<SourceProvider>()
            .StrictMode(true)
            .RuleFor(x => x.Id, f => Guid.NewGuid())
            .RuleFor(x => x.Name, f => f.Person.FirstName)
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.TeamId, f => null)
            .RuleFor(x => x.Team, f => null)
            .RuleFor(x => x.GithubProvider, f => null)
            .RuleFor(x => x.GithubProviderId, f => githubProviderId)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);
    }

    public static Faker<Deployment> DeploymentFaker(Stack Stack)
    {
        return new Faker<Deployment>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.Stack, f => null)
            .RuleFor(x => x.Logs, f => new List<DeploymentLog>())
            .RuleFor(x => x.State, DeploymentState.Created)
            .RuleFor(x => x.RecipeYaml, f => null)
            .RuleFor(x => x.ServerId, f => Stack.ServerId)
            .RuleFor(x => x.CreatedBy, f => null)
            .RuleFor(x => x.LastModifiedBy, f => null)
            .RuleFor(x => x.StackId, Stack.Id)
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);
    }

    public static Faker<Stack> StackFaker(
        Guid environmentId,
        PublicGitSettings settings,
        Action<Faker<Server>>? additionalServerActions = null
    )
    {
        var stack = StackFaker(environmentId, additionalServerActions);

        var source = new ApplicationSource
        {
            Type = ApplicationSourceType.Git,
            Git = new ApplicationSourceGit
            {
                Repository = settings.Repo,
                Branch = settings.Branch,
                Path = settings.Path,
            },
            GithubApp = null,
        };

        return stack.RuleFor(x => x.Source, f => source);
    }

    public static Faker<Stack> StackFaker(
        Guid environmentId,
        Action<Faker<Server>>? additionalServerActions = null
    )
    {
        var serverFaker = ServerFaker;
        additionalServerActions?.Invoke(serverFaker);
        var server = serverFaker.Generate();
        return new Faker<Stack>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.Name, f => $"Application {f.Person.FirstName}")
            .RuleFor(x => x.ServerId, server.Id)
            .RuleFor(x => x.EnvironmentVariables, f => new List<StackEnvironmentVariable>())
            .RuleFor(x => x.BuildPack, f => StackBuildPack.Nixpacks)
            .RuleFor(x => x.CreatedBy, (Guid?)null)
            .RuleFor(x => x.Description, f => null)
            .RuleFor(x => x.LastModifiedBy, (Guid?)null)
            .RuleFor(x => x.Server, server)
            // ReSharper disable once RedundantCast
            .RuleFor(x => x.Environment, f => (Environment?)null)
            .RuleFor(x => x.DockerComposeData, f => new StackDockerComposeData())
            .RuleFor(x => x.EnvironmentId, environmentId)
            .RuleFor(x => x.Deployments, f => new List<Deployment>())
            .RuleFor(x => x.Source, f => null)
            .RuleFor(x => x.HealthStatus, f => new StackHealthEntity())
            .RuleFor(x => x.Services, f => new List<StackService>())
            .RuleFor(
                x => x.EnvironmentVariableGroups,
                f => new List<StackEnvironmentVariableGroup>()
            )
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);
    }

    public static Faker<StackServiceVolume> StackServiceVolumeFaker(Guid stackServiceId)
    {
        return new Faker<StackServiceVolume>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow)
            .RuleFor(x => x.StackServiceId, stackServiceId)
            .RuleFor(x => x.StackService, (StackService?)null)
            .RuleFor(x => x.CreatedBy, (Guid?)null)
            .RuleFor(x => x.LastModifiedBy, (Guid?)null)
            .RuleFor(x => x.Name, f => f.Person.FullName)
            .RuleFor(x => x.SourcePath, f => f.Internet.UrlRootedPath())
            .RuleFor(x => x.DestinationPath, f => f.Internet.UrlRootedPath())
            .RuleFor(x => x.Type, f => f.PickRandom<StackServiceVolumeType>());
    }

    public static Faker<StackService> StackServiceFaker(Stack stack)
    {
        return new Faker<StackService>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow)
            .RuleFor(x => x.StackId, stack.Id)
            .RuleFor(x => x.Description, f => null)
            .RuleFor(x => x.DockerComposeData, f => new StackServiceDockerComposeData())
            .RuleFor(x => x.HealthcheckConfiguration, f => new EntityHealthcheckConfiguration())
            .RuleFor(x => x.StackServiceTypeId, StackServiceTypeConstants.App)
            .RuleFor(x => x.Stack, (Stack?)null)
            .RuleFor(x => x.HttpConfiguration, (EntityHttpConfiguration?)null)
            .RuleFor(x => x.Volumes, new List<StackServiceVolume>())
            .RuleFor(x => x.CreatedBy, (Guid?)null)
            .RuleFor(x => x.LastModifiedBy, (Guid?)null)
            .RuleFor(x => x.Name, f => f.Person.FullName)
            .RuleFor(x => x.Type, (StackServiceType?)null)
            .RuleFor(x => x.HealthStatus, f => EntityHealthStatusFaker.Generate());
    }

    public static Faker<Environment> EnvironmentFaker(Project project)
    {
        return new Faker<Environment>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            // ReSharper disable once RedundantCast
            .RuleFor(x => x.Project, f => (Project?)null)
            .RuleFor(x => x.ProjectId, project.Id)
            .RuleFor(x => x.Name, f => $"Environment {f.Person.FirstName}")
            .RuleFor(x => x.Stacks, f => new List<Stack>())
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);
    }

    public static Faker<Team> TeamFaker()
    {
        return new Faker<Team>()
            .StrictMode(true)
            .RuleFor(x => x.Id, Guid.NewGuid)
            .RuleFor(x => x.Name, f => f.Person.FullName)
            .RuleFor(x => x.Owner, (User?)null)
            .RuleFor(x => x.OwnerId, f => Guid.Empty)
            .RuleFor(x => x.CreatedAt, DateTime.UtcNow)
            .RuleFor(x => x.TeamUsers, f => new List<TeamUser>())
            .RuleFor(x => x.Projects, f => new List<Project>())
            .RuleFor(x => x.CreatedBy, f => null)
            .RuleFor(x => x.LastModifiedBy, f => null)
            .RuleFor(x => x.UpdatedAt, DateTime.UtcNow);
    }

    public record PublicGitSettings(string Repo, string Branch, string Path);
}
