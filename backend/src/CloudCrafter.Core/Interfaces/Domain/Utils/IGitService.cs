using CloudCrafter.Domain.Domain.Utils;
using CloudCrafter.Domain.Entities;

namespace CloudCrafter.Core.Interfaces.Domain.Utils;

public interface IGitService
{
    Task<GitRepositoryCheckResultDto> ValidateRepository(
        string repository,
        string? path = null,
        string? branch = null
    );

    Task<GitRepositoryCheckResultDto> ValidateSourceProviderBranch(Guid stackId, string branchName);

    Task<(string? DockerComposeContent, string? Error)> FetchDockerComposeContent(Stack stack);
}
