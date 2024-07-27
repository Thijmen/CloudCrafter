using System.CommandLine;
using CloudCrafter.Agent.Runner;
using CloudCrafter.Agent.Runner.Cli;
using CloudCrafter.Agent.Runner.Common.Behaviour;
using CloudCrafter.Agent.Runner.DeploymentLogPump;
using CloudCrafter.Agent.Runner.DeploymentLogPump.Implementation;
using CloudCrafter.Agent.Runner.RunnerEngine.Deployment;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        return await RunApp(args);
    }

    public static async Task<int> RunApp(string[] args)
    {
        var host = CreateHostBuilder(args).Build();

        var demoOption = new Option<bool>("--demo", "Runs the dummy deployment recipe");
        var fromBase64Option = new Option<string>("--from-base64", "Runs the deployment recipe from a base64 string");
        
        var rootCommand = new RootCommand("CloudCrafter Agent");
        rootCommand.AddOption(demoOption);

        int? resultCode = null;
        rootCommand.SetHandler(async (demoOptionValue, fromBase64OptionValue) =>
        {
            var agentRunner = new AgentRunner(host);
            resultCode = await agentRunner.Run(demoOptionValue, fromBase64OptionValue);
        }, demoOption, fromBase64Option);


        var intResult = await rootCommand.InvokeAsync(args);

        return resultCode ?? intResult;
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddDeploymentStepsConfig();
                services.AddSingleton<IMessagePump, MessagePump>();
                services.AddTransient<ICommandExecutor, CommandExecutor>();
                services.AddSingleton<IDeploymentStepFactory, DeploymentStepFactory>();

                services.AddTransient<DeploymentService>();
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
                    cfg.RegisterServicesFromAssembly(typeof(IAgentRunnerTarget).Assembly);

                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehaviour<,>));
                });
            });
    }
}
