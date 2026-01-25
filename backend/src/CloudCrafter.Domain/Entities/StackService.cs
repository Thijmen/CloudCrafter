using CloudCrafter.Domain.Common;

namespace CloudCrafter.Domain.Entities;

public class StackService : BaseAuditableEntity
{
    public required Guid StackId { get; init; }
    public required Guid StackServiceTypeId { get; set; }

    public Stack Stack { get; set; } = null!;
    public required EntityHttpConfiguration? HttpConfiguration { get; set; }
    public required EntityHealthcheckConfiguration HealthcheckConfiguration { get; set; } = new();
    public List<StackServiceVolume> Volumes { get; set; } = new();
    public StackServiceType Type { get; set; } = null!;

    public required string Name { get; set; }
    public required string? Description { get; init; }
    public StackServiceDockerComposeData DockerComposeData { get; set; } = new();

    public required EntityStackServiceHealthStatus HealthStatus { get; init; }
}

public class StackServiceDockerComposeData
{
    /// <summary>
    ///     Used to identify services in docker-compose files, to re-use the StackService
    /// </summary>
    public string? ServiceName { get; set; }
}
