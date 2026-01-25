using CloudCrafter.Core.Events.DomainEvents;
using CloudCrafter.Core.Interfaces.Domain.Stacks;
using CloudCrafter.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace CloudCrafter.Core.Events.Handlers;

public class StackCreatedEventHandler(
    ILogger<StackCreatedEventHandler> logger,
    IStackServicesService stackServicesService,
    IStackEnvironmentVariablesService stackEnvironmentVariablesService,
    IStacksService stacksService
) : INotificationHandler<StackCreatedEvent>
{
    public async Task Handle(StackCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Stack created: {StackId}", notification.Stack.Id);

        if (notification.Stack.BuildPack == StackBuildPack.Nixpacks)
        {
            await HandleNixpacks(notification.Stack);
        }

        if (notification.Stack.BuildPack == StackBuildPack.DockerCompose)
        {
            await HandleDockerCompose(notification.Stack);
        }

        await CreateDefaultEnvironmentVariables(notification.Stack);
    }

    private async Task CreateDefaultEnvironmentVariables(Stack stack)
    {
        await stackEnvironmentVariablesService.CreateDefaultVariableGroups(stack.Id);
    }

    private async Task HandleNixpacks(Stack stack)
    {
        logger.LogInformation("Adding default App service to stack: {StackId}", stack.Id);

        await stackServicesService.AddAppServiceToStack(stack.Id, "Default");

        logger.LogInformation("Default App service added to stack: {StackId}", stack.Id);
    }

    private async Task HandleDockerCompose(Stack stack)
    {
        logger.LogInformation("Creating Docker compose services for stack: {StackId}", stack.Id);

        await stacksService.FetchAndLoadServices(stack.Id);

        logger.LogInformation("Default App service added to stack: {StackId}", stack.Id);
    }
}
