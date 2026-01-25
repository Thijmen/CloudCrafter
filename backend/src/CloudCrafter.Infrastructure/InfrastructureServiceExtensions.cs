using System.Text;
using Ardalis.GuardClauses;
using CloudCrafter.Core.Common.Interfaces;
using CloudCrafter.Core.Interfaces;
using CloudCrafter.Core.Interfaces.Domain.Auth;
using CloudCrafter.Core.Interfaces.Domain.Stacks;
using CloudCrafter.Core.Interfaces.Repositories;
using CloudCrafter.Core.Services.Domain.Stacks;
using CloudCrafter.Domain.Constants;
using CloudCrafter.Domain.Entities;
using CloudCrafter.Infrastructure.Core.Configuration;
using CloudCrafter.Infrastructure.Data;
using CloudCrafter.Infrastructure.Data.Interceptors;
using CloudCrafter.Infrastructure.Email;
using CloudCrafter.Infrastructure.Identity;
using CloudCrafter.Infrastructure.Identity.Services;
using CloudCrafter.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CloudCrafter.Infrastructure;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddCloudCrafterConfiguration(this IServiceCollection services)
    {
        services
            .AddOptions<CloudCrafterConfig>()
            .BindConfiguration(CloudCrafterConfig.KEY)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration config
    )
    {
        var connectionString = config.GetConnectionString("PostgresConnection");
        Guard.Against.Null(connectionString, "PostgresConnection is not set.");

        services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        services.AddDbContext<AppDbContext>(
            (sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                options.UseNpgsql(connectionString);
            }
        );

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<AppDbContext>()
        );
        services.AddScoped<ApplicationDbContextInitialiser>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(x =>
            {
                var jwtSettings = new JwtSettings();
                config.Bind(JwtSettings.KEY, jwtSettings);
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSettings.SecretKey)
                    ),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ClockSkew = TimeSpan.FromSeconds(5),
                };
            });

        services.AddAuthorizationBuilder();

        services
            .AddIdentityCore<User>()
            .AddRoles<Role>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddApiEndpoints();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<AuthSeeder>();
        services.AddTransient<IIdentityService, IdentityService>();

        services.AddAuthorization(options =>
            options.AddPolicy(Policies.CanPurge, policy => policy.RequireRole(Roles.Administrator))
        );

        // Add services
        services
            .AddScoped<ICloudCrafterAuthService, CloudCrafterAuthService>()
            .AddScoped<IUserRepository, UserRepository>()
            .AddScoped<IServerRepository, ServerRepository>()
            .AddScoped<IEnvironmentRepository, EnvironmentRepository>()
            .AddScoped<IProviderRepository, ProviderRepository>()
            .AddScoped<ITeamsRepository, TeamsRepository>()
            .AddScoped<IProjectRepository, ProjectRepository>()
            .AddScoped<IDeploymentRepository, DeploymentRepository>()
            .AddScoped<IStackRepository, StackRepository>()
            .AddScoped<IUserRefreshTokenRepository, UserRefreshTokenRepository>()
            .AddScoped<IJwtService, JwtService>()
            .AddScoped<IEmailSender, FakeEmailSender>()
            .AddScoped<IStackEnvironmentVariablesService, StackEnvironmentVariablesService>()
            .AddScoped<IStackServiceProvisioner, StackServiceProvisioner>()
            .AddScoped<IStackServiceVolumesService, StackServiceVolumesService>();

        return services;
    }
}
