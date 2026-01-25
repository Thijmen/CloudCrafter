using CloudCrafter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCrafter.Infrastructure.Data.Config;

public class StackServiceConfiguration : IEntityTypeConfiguration<StackService>
{
    public void Configure(EntityTypeBuilder<StackService> builder)
    {
        builder.OwnsOne(service => service.HealthStatus);
        builder.OwnsOne(service => service.DockerComposeData);
        builder.OwnsOne(service => service.HttpConfiguration);
        builder.OwnsOne(service => service.HealthcheckConfiguration);
    }
}
