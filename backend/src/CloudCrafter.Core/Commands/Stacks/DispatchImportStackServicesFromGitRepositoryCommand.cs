using CloudCrafter.Core.Common.Interfaces.Access;
using CloudCrafter.Core.Common.Security;
using CloudCrafter.Core.Interfaces.Domain.Stacks;
using MediatR;

namespace CloudCrafter.Core.Commands.Stacks;

[Authorize]
public record DispatchImportStackServicesFromGitRepositoryCommand : IRequest, IRequireStackAccess
{
    public Guid StackId { get; init; }
}

internal class DispatchImportStackServicesFromGitRepositoryCommandHandler(
    IStacksService stacksService
) : IRequestHandler<DispatchImportStackServicesFromGitRepositoryCommand>
{
    public async Task Handle(
        DispatchImportStackServicesFromGitRepositoryCommand request,
        CancellationToken cancellationToken
    )
    {
        await stacksService.FetchAndLoadServices(request.StackId);
    }
}
