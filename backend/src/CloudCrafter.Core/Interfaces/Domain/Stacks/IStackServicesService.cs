using CloudCrafter.Core.Commands.Stacks.Service;
using CloudCrafter.DockerCompose.Engine.Yaml;
using CloudCrafter.Domain.Domain.Stack;
using CloudCrafter.Domain.Entities;

namespace CloudCrafter.Core.Interfaces.Domain.Stacks;

public interface IStackServicesService
{
    Task AddAppServiceToStack(Guid stackId, string name);
    Task AddOrUpdateServiceFromServiceEditor(
        Stack stack,
        DockerComposeEditor.ServiceEditor serviceEditor
    );
    Task<StackServiceDto?> UpdateStackService(UpdateStackServiceCommand request);
}
