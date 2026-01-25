using System.ComponentModel.DataAnnotations;
using CloudCrafter.Core.Common.Interfaces.Access;
using CloudCrafter.Core.Common.Security;
using CloudCrafter.Core.Interfaces.Domain.Stacks;
using CloudCrafter.Domain.Domain.Stack;
using MediatR;

namespace CloudCrafter.Core.Commands.Stacks;

[Authorize]
public class CreateStackCommand
    : IRequest<StackCreatedDto>,
        IRequireServerAccess,
        IRequireEnvironmentAccess
{
    [MinLength(3)]
    public required string Name { get; init; }

    [MinLength(1)]
    public required string GitRepository { get; init; }

    [MinLength(1)]
    public required string GitBranch { get; init; }

    public required string PathInGitRepository { get; init; }

    public required CreateStackBuildOption BuildOption { get; init; }

    public required Guid EnvironmentId { get; set; }

    public required Guid ServerId { get; set; }
}

internal class CreateStackCommandHandler(IStacksService service)
    : IRequestHandler<CreateStackCommand, StackCreatedDto>
{
    public async Task<StackCreatedDto> Handle(
        CreateStackCommand request,
        CancellationToken cancellationToken
    )
    {
        var args = new CreateStackArgsDto
        {
            Name = request.Name,
            EnvironmentId = request.EnvironmentId,
            ServerId = request.ServerId,
            PublicGit = new CreateStackPublicGitRepo
            {
                GitRepository = request.GitRepository,
                Branch = request.GitBranch,
                Path = request.PathInGitRepository,
            },
            BuildOption = request.BuildOption,
        };

        return await service.CreateStack(args);
    }
}
