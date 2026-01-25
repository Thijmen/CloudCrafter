using AutoMapper;
using CloudCrafter.Core.Commands.Stacks.Service;
using CloudCrafter.Core.Common.Interfaces;
using CloudCrafter.Core.Interfaces.Domain.Stacks;
using CloudCrafter.Core.Interfaces.Domain.Users;
using CloudCrafter.Core.Interfaces.Repositories;
using CloudCrafter.DockerCompose.Engine.Yaml;
using CloudCrafter.Domain.Domain.Application.Services;
using CloudCrafter.Domain.Domain.Stack;
using CloudCrafter.Domain.Domain.User.ACL;
using CloudCrafter.Domain.Entities;

namespace CloudCrafter.Core.Services.Domain.Stacks;

public class StackServicesService(
    IStackRepository stackRepository,
    IMapper mapper,
    IUserAccessService userAccessService,
    IUser user
) : IStackServicesService
{
    public async Task AddAppServiceToStack(Guid stackId, string name)
    {
        await stackRepository.AddServiceToStack(stackId, name, StackServiceTypes.App, true);
    }

    public async Task AddOrUpdateServiceFromServiceEditor(
        Stack stack,
        DockerComposeEditor.ServiceEditor serviceEditor
    )
    {
        var name = serviceEditor.ServiceName();

        var stackService = stack.Services.FirstOrDefault(x =>
            x.DockerComposeData.ServiceName == name
        );

        if (stackService == null)
        {
            // TODO: Find out type
            stackService = await stackRepository.AddServiceToStack(
                stack.Id,
                name,
                StackServiceTypes.DatabasePostgres,
                false
            );
        }

        stackService.DockerComposeData.ServiceName = name;
    }

    public async Task<StackServiceDto?> UpdateStackService(UpdateStackServiceCommand request)
    {
        var stack = await stackRepository.GetStack(request.StackId);

        if (stack == null)
        {
            return null;
        }

        await userAccessService.EnsureHasAccessToEntity(
            stack.Environment.Project,
            user?.Id,
            AccessType.Write
        );

        var stackService = await stackRepository.GetService(request.StackServiceId);

        if (stackService == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            stackService.Name = request.Name;
        }

        if (request.DomainName != null)
        {
            // Note the check - it can be an empty string!
            stackService.HttpConfiguration ??= new EntityHttpConfiguration
            {
                DomainName = request.Name,
                ContainerHttpPort = null,
            };

            stackService.HttpConfiguration.DomainName = request.DomainName;
        }

        if (request.ContainerPortExposes.HasValue)
        {
            stackService.HttpConfiguration ??= new EntityHttpConfiguration
            {
                DomainName = null,
                ContainerHttpPort = null,
            };

            stackService.HttpConfiguration.ContainerHttpPort = request.ContainerPortExposes;
        }

        if (request.ContainerHealthCheckPort.HasValue)
        {
            stackService.HealthcheckConfiguration.HttpPort = request.ContainerHealthCheckPort;
        }

        await stackRepository.SaveChangesAsync();

        return mapper.Map<StackServiceDto>(stackService);
    }
}
