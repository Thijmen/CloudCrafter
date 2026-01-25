using Ardalis.GuardClauses;
using AutoMapper;
using CloudCrafter.Agent.SignalR.Models;
using CloudCrafter.Core.Commands.Stacks;
using CloudCrafter.Core.Common.Interfaces;
using CloudCrafter.Core.Common.Responses;
using CloudCrafter.Core.Events.DomainEvents;
using CloudCrafter.Core.Interfaces.Domain.Stacks;
using CloudCrafter.Core.Interfaces.Domain.Stacks.Filters;
using CloudCrafter.Core.Interfaces.Domain.Users;
using CloudCrafter.Core.Interfaces.Domain.Utils;
using CloudCrafter.Core.Interfaces.Repositories;
using CloudCrafter.Domain.Common;
using CloudCrafter.Domain.Domain.Deployment;
using CloudCrafter.Domain.Domain.Stack;
using CloudCrafter.Domain.Domain.Stack.Filter;
using CloudCrafter.Domain.Domain.User.ACL;
using CloudCrafter.Domain.Entities;
using CloudCrafter.Domain.Requests.Filtering;

namespace CloudCrafter.Core.Services.Domain.Stacks;

public class StacksService(
    IStackRepository repository,
    IServerRepository serverRepository,
    IMapper mapper,
    IUserAccessService userAccessService,
    IUser user,
    IGitService gitService,
    IStackServiceProvisioner stackServiceProvisioner
) : IStacksService
{
    public async Task<StackCreatedDto> CreateStack(CreateStackArgsDto args)
    {
        var server = await serverRepository.GetServerEntityOrFail(args.ServerId);

        await userAccessService.EnsureHasAccessToEntity(server, user?.Id, AccessType.Write);

        var createdStack = await repository.CreateStack(args);

        return new StackCreatedDto { Id = createdStack.Id };
    }

    public async Task<SimpleStackDetailsDto?> GetSimpleStackDetails(Guid id)
    {
        var stack = await repository.GetStack(id);

        if (stack == null)
        {
            return null;
        }

        return mapper.Map<SimpleStackDetailsDto>(stack);
    }

    public async Task<StackDetailDto?> GetStackDetail(Guid id)
    {
        var stack = await repository.GetStack(id);

        if (stack == null)
        {
            return null;
        }

        await userAccessService.EnsureHasAccessToEntity(
            stack.Environment.Project,
            user?.Id,
            AccessType.Read
        );

        return mapper.Map<StackDetailDto>(stack);
    }

    public async Task EnsureStackWritePermissions(Guid stackId)
    {
        var stack = await repository.GetStack(stackId);

        if (stack == null)
        {
            throw new NotFoundException("Stack", "Stack not found");
        }

        await userAccessService.EnsureHasAccessToEntity(
            stack.Environment.Project,
            user?.Id,
            AccessType.Write
        );
    }

    public Task<Guid> CreateDeployment(Guid stackId)
    {
        return repository.CreateDeployment(stackId);
    }

    public async Task<List<SimpleDeploymentDto>> GetDeployments(DeploymentsFilter filter)
    {
        var deployments = await repository.GetDeployments(filter);

        return mapper.Map<List<SimpleDeploymentDto>>(deployments);
    }

    public Task<PaginatedList<SimpleDeploymentDto>> GetDeploymentsPaginated(
        DeploymentsFilter filter,
        BasePaginationRequest paginatedRequest
    )
    {
        return repository.GetDeploymentsPaginated(filter, paginatedRequest);
    }

    public async Task HandleHealthChecks(Guid serverId, ContainerHealthCheckArgs args)
    {
        foreach (var stackInfo in args.Info)
        {
            var stackId = stackInfo.Key;

            var stackEntity = await repository.GetStack(stackId);

            if (stackEntity == null || stackEntity.ServerId != serverId)
            {
                continue;
            }

            var allAreHealthy = stackInfo.Value.StackServices.All(x =>
                x.Value.Status == ContainerHealthCheckStackInfoHealthStatus.Healthy
            );

            var allAreUnhealthy = stackInfo.Value.StackServices.All(x =>
                x.Value.Status == ContainerHealthCheckStackInfoHealthStatus.Unhealthy
            );
            stackEntity.HealthStatus.SetStatus(
                allAreHealthy ? EntityHealthStatusValue.Healthy
                : allAreUnhealthy ? EntityHealthStatusValue.Unhealthy
                : EntityHealthStatusValue.Degraded
            );

            stackEntity.AddDomainEvent(
                DomainEventDispatchTiming.AfterSaving,
                new StackHealthUpdatedEvent(stackEntity)
            );
            foreach (var stackService in stackInfo.Value.StackServices)
            {
                var stackServiceId = stackService.Key;

                // TODO: Move this to Unit of Work
                var stackServiceEntity = await repository.GetService(stackServiceId);

                if (stackServiceEntity?.Stack?.ServerId != serverId)
                {
                    continue;
                }

                var isRunning = stackService.Value.IsRunning;

                stackServiceEntity?.HealthStatus.SetStatus(
                    stackService.Value.Status == ContainerHealthCheckStackInfoHealthStatus.Healthy
                            ? EntityHealthStatusValue.Healthy
                        : stackService.Value.Status
                        == ContainerHealthCheckStackInfoHealthStatus.Unhealthy
                            ? EntityHealthStatusValue.Unhealthy
                        : EntityHealthStatusValue.Degraded,
                    isRunning
                );
            }
        }

        await repository.SaveChangesAsync();
    }

    public async Task<StackDetailDto?> UpdateStack(UpdateStackCommand request)
    {
        var stack = await repository.GetStack(request.StackId);

        if (stack == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            stack.Name = request.Name;
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            stack.Description = request.Description;
        }

        if (stack.Source?.Git != null)
        {
            // Update git related settings

            if (!string.IsNullOrWhiteSpace(request.GitPublicSettings?.Repository))
            {
                stack.Source.Git.Repository = request.GitPublicSettings.Repository;
            }

            if (!string.IsNullOrWhiteSpace(request.GitPublicSettings?.Branch))
            {
                stack.Source.Git.Branch = request.GitPublicSettings.Branch;
            }

            if (!string.IsNullOrWhiteSpace(request.GitPublicSettings?.Path))
            {
                stack.Source.Git.Path = request.GitPublicSettings.Path;
            }
        }

        if (stack.Source?.GithubApp != null)
        {
            if (!string.IsNullOrWhiteSpace(request.GithubSettings?.Branch))
            {
                stack.Source.GithubApp.Branch = request.GithubSettings.Branch;
            }

            if (!string.IsNullOrWhiteSpace(request.GithubSettings?.Path))
            {
                stack.Source.GithubApp.Path = request.GithubSettings.Path;
            }
        }

        stack.AddDomainEvent(DomainEventDispatchTiming.AfterSaving, new StackUpdatedEvent(stack));

        await repository.SaveChangesAsync();

        return mapper.Map<StackDetailDto>(stack);
    }

    public async Task MarkStacksUnknownAfterTimespan(TimeSpan maxHealthCheckAge)
    {
        var stacks = await repository.FilterStacks(
            new StackFilter { HealthCheckAgeOlderThan = maxHealthCheckAge }
        );

        foreach (var stack in stacks)
        {
            stack.HealthStatus.SetStatusWithoutDate(EntityHealthStatusValue.HealthCheckOverdue);

            foreach (var service in stack.Services)
            {
                if (
                    !service.HealthStatus.StatusAt.HasValue
                    || service.HealthStatus.StatusAt.Value < DateTime.UtcNow - maxHealthCheckAge
                )
                {
                    service.HealthStatus.SetStatusWithoutDate(
                        EntityHealthStatusValue.HealthCheckOverdue
                    );
                }
            }
        }

        await repository.SaveChangesAsync();
    }

    public async Task FetchAndLoadServices(Guid stackId)
    {
        var stack = await repository.GetStack(stackId);
        if (stack == null)
        {
            throw new NotFoundException("Stack", "Stack not found");
        }

        await userAccessService.EnsureHasAccessToEntity(
            stack.Environment.Project,
            user?.Id,
            AccessType.Write
        );

        if (stack.BuildPack != StackBuildPack.DockerCompose)
        {
            throw new InvalidOperationException("Stack is not a Docker Compose stack");
        }

        var (dockerComposeContent, error) = await gitService.FetchDockerComposeContent(stack);

        if (error != null)
        {
            throw new InvalidOperationException($"Failed to fetch docker-compose content: {error}");
        }

        if (string.IsNullOrEmpty(dockerComposeContent))
        {
            throw new InvalidOperationException("No docker-compose content found");
        }

        await stackServiceProvisioner.ProvisionStackFromYaml(stack.Id, dockerComposeContent);
    }

    public async Task DeleteStack(Guid stackId)
    {
        var stack = await repository.GetStack(stackId);

        if (stack == null)
        {
            throw new NotFoundException("Stack", "Stack not found");
        }

        await userAccessService.EnsureHasAccessToEntity(
            stack.Environment.Project,
            user?.Id,
            AccessType.Write
        );

        repository.DeleteStack(stack);

        await repository.SaveChangesAsync();
    }
}
