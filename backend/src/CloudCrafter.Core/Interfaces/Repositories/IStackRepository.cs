using CloudCrafter.Core.Common.Responses;
using CloudCrafter.Core.Interfaces.Domain.Stacks.Filters;
using CloudCrafter.Domain.Domain.Application.Services;
using CloudCrafter.Domain.Domain.Deployment;
using CloudCrafter.Domain.Domain.Stack;
using CloudCrafter.Domain.Domain.Stack.Filter;
using CloudCrafter.Domain.Entities;
using CloudCrafter.Domain.Requests.Filtering;

namespace CloudCrafter.Core.Interfaces.Repositories;

public interface IStackRepository
{
    Task<Stack> CreateStack(CreateStackArgsDto args);
    Task<Stack?> GetStack(Guid id);

    Task<StackService> AddServiceToStack(
        Guid stackId,
        string name,
        StackServiceTypes type,
        bool shouldPersist
    );

    Task<Guid> CreateDeployment(Guid stackId);
    Task<StackService?> GetService(Guid stackServiceId);
    Task<List<Deployment>> GetDeployments(DeploymentsFilter filter);

    Task<List<Stack>> FilterStacks(StackFilter filter);

    Task SaveChangesAsync();

    Task<PaginatedList<SimpleDeploymentDto>> GetDeploymentsPaginated(
        DeploymentsFilter filter,
        BasePaginationRequest paginatedRequest
    );

    Task AddEnvironmentVariable(StackEnvironmentVariable variable, bool persist = true);
    Task<List<StackEnvironmentVariable>> GetEnvironmentVariables(Guid stackId);

    Task AddEnvironmentVariableGroups(IList<StackEnvironmentVariableGroup> groups);

    Task<List<StackEnvironmentVariableGroup>> GetEnvironmentVariableGroups(Guid stackId);

    /// <summary>
    ///     Gets a specific environment variable group
    /// </summary>
    /// <param name="stackId">ID of the stack</param>
    /// <param name="groupId">ID of the environment variable group</param>
    /// <returns>The environment variable group, or null if not found</returns>
    Task<StackEnvironmentVariableGroup?> GetEnvironmentVariableGroup(Guid stackId, Guid groupId);

    /// <summary>
    ///     Removes an environment variable group
    /// </summary>
    /// <param name="group">The environment variable group to remove</param>
    void RemoveEnvironmentVariableGroup(StackEnvironmentVariableGroup group);

    /// <summary>
    ///     Gets a service volume directly from the database
    /// </summary>
    /// <param name="stackId">ID of the stack</param>
    /// <param name="serviceId">ID of the service</param>
    /// <param name="volumeId">ID of the volume</param>
    /// <returns>The service volume, or null if not found</returns>
    Task<StackServiceVolume?> GetServiceVolume(Guid stackId, Guid serviceId, Guid volumeId);

    /// <summary>
    ///     Adds a new service volume to the database
    /// </summary>
    /// <param name="volume">The volume to add</param>
    void AddStackServiceVolume(StackServiceVolume volume);

    Task<bool> StackServiceExists(Guid stackId, Guid stackServiceId);
    Task<List<StackServiceVolume>> GetServiceVolumes(Guid stackId, Guid stackServiceId);
    void DeleteStackServiceVolume(StackServiceVolume volume);
    void AddService(StackService service);
    void DeleteStack(Stack stack);
}
