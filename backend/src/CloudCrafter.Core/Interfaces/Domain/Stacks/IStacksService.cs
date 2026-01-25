using CloudCrafter.Agent.SignalR.Models;
using CloudCrafter.Core.Commands.Stacks;
using CloudCrafter.Core.Common.Responses;
using CloudCrafter.Core.Interfaces.Domain.Stacks.Filters;
using CloudCrafter.Domain.Domain.Deployment;
using CloudCrafter.Domain.Domain.Stack;
using CloudCrafter.Domain.Requests.Filtering;

namespace CloudCrafter.Core.Interfaces.Domain.Stacks;

public interface IStacksService
{
    Task<StackCreatedDto> CreateStack(CreateStackArgsDto args);
    Task<SimpleStackDetailsDto?> GetSimpleStackDetails(Guid id);
    Task<StackDetailDto?> GetStackDetail(Guid id);

    Task EnsureStackWritePermissions(Guid stackId);

    Task<Guid> CreateDeployment(Guid stackId);
    Task<List<SimpleDeploymentDto>> GetDeployments(DeploymentsFilter filter);

    Task<PaginatedList<SimpleDeploymentDto>> GetDeploymentsPaginated(
        DeploymentsFilter filter,
        BasePaginationRequest paginatedRequest
    );

    Task HandleHealthChecks(Guid serverId, ContainerHealthCheckArgs args);
    Task<StackDetailDto?> UpdateStack(UpdateStackCommand request);

    Task MarkStacksUnknownAfterTimespan(TimeSpan maxHealthCheckAge);
    Task FetchAndLoadServices(Guid stackId);
    Task DeleteStack(Guid stackId);
}
