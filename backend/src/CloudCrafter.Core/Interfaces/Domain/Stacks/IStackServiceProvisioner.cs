namespace CloudCrafter.Core.Interfaces.Domain.Stacks;

public interface IStackServiceProvisioner
{
    Task ProvisionStackFromYaml(Guid stackId, string? yaml);
}
