using CloudCrafter.Domain.Domain.Application.Services;
using CloudCrafter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCrafter.Infrastructure.Data.Config;

public class StackServiceTypeConfiguration : IEntityTypeConfiguration<StackServiceType>
{
    public void Configure(EntityTypeBuilder<StackServiceType> builder)
    {
        builder.HasData(
            new List<StackServiceType>
            {
                new()
                {
                    Id = StackServiceTypeConstants.App,
                    Type = nameof(StackServiceTypeConstants.App),
                },
                new()
                {
                    Id = StackServiceTypeConstants.DatabasePostgres,
                    Type = nameof(StackServiceTypeConstants.DatabasePostgres),
                },
            }
        );
    }
}
