using System.Reflection;
using CloudCrafter.Agent.Models.Deployment;
using CloudCrafter.Agent.Models.Deployment.Steps;
using CloudCrafter.Agent.Models.Recipe;
using CloudCrafter.Agent.Runner.Cli;
using CloudCrafter.Agent.Runner.Cli.Helpers;
using CloudCrafter.Agent.Runner.Cli.Helpers.Abstraction;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment;
using Microsoft.Extensions.DependencyInjection;

namespace CloudCrafter.Agent.Runner;

public static class ExtensionMethods
{
    public static IServiceCollection AddDeploymentStepsConfig(this IServiceCollection services)
    {
        var assembly = typeof(IAgentRunnerTarget).Assembly;


        var handlerTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IDeploymentStepHandler<>)));

        var configTypes = assembly.GetTypes()
            .Where(t => t.GetInterfaces().Any(i =>
                i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(IDeploymentStepConfig<>)));

        foreach (var handlerType in handlerTypes)
        {
            var interfaceType = handlerType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeploymentStepHandler<>));
            var stepType = GetDeploymentBuildStepType(handlerType);
            services.AddKeyedTransient(interfaceType, stepType, handlerType);
        }

        foreach (var configType in configTypes)
        {
            var interfaceType = configType.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDeploymentStepConfig<>));
            var stepType = GetDeploymentBuildStepType(configType);
            services.AddKeyedTransient(interfaceType, stepType, configType);
        }

        services.AddTransient<INixpacksHelper, NixpacksHelper>()
            .AddTransient<IDockerHealthCheckHelper, DockerHealthCheckHelper>()
            .AddTransient<IDockerHelper, DockerHelper>()
            .AddTransient<IDockerClientProvider, DockerClientProvider>()
            .AddTransient<IDockerComposeHelper, DockerComposeHelper>();
        services.AddTransient<ICommandParser, CommandParser>()
            .AddTransient<IFileSystemHelper, FileSystemHelper>();

        return services;
    }

    private static DeploymentBuildStepType GetDeploymentBuildStepType(Type type)
    {
        var attribute = type.GetCustomAttribute<DeploymentStepAttribute>();
        if (attribute == null)
        {
            throw new InvalidOperationException($"Type {type.Name} is missing DeploymentStepAttribute");
        }

        return attribute.StepType;
    }
}
