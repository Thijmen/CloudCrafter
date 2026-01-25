using CloudCrafter.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CloudCrafter.Infrastructure.Data.Config;

public class StackConfiguration : IEntityTypeConfiguration<Stack>
{
    public void Configure(EntityTypeBuilder<Stack> builder)
    {
        builder.OwnsOne(stack => stack.HealthStatus);
        builder.OwnsOne(stack => stack.DockerComposeData);

        // Configure one-to-many relationship with StackEnvironmentVariable
        builder
            .HasMany(s => s.EnvironmentVariables)
            .WithOne(v => v.Stack)
            .HasForeignKey(v => v.StackId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(s => s.EnvironmentVariableGroups)
            .WithOne(v => v.Stack)
            .HasForeignKey(v => v.StackId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
