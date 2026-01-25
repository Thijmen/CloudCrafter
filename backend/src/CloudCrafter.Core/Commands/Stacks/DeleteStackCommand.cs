using CloudCrafter.Core.Common.Interfaces.Access;
using CloudCrafter.Core.Common.Security;
using CloudCrafter.Core.Interfaces.Domain.Stacks;
using MediatR;

namespace CloudCrafter.Core.Commands.Stacks;

[Authorize]
public record DeleteStackCommand : IRequest, IRequireStackAccess
{
    public required Guid StackId { get; init; }
}

internal class DeleteStackCommandHandler(IStacksService stacksService)
    : IRequestHandler<DeleteStackCommand>
{
    public Task Handle(DeleteStackCommand request, CancellationToken cancellationToken)
    {
        return stacksService.DeleteStack(request.StackId);
    }
}
