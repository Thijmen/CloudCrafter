﻿using System.Reflection;
using Ardalis.ListStartupServices;
using Ardalis.SharedKernel;
using CloudCrafter.Core.ContributorAggregate;
using CloudCrafter.Core.Interfaces;
using CloudCrafter.Infrastructure;
using CloudCrafter.Infrastructure.Data;
using CloudCrafter.Infrastructure.Email;
using CloudCrafter.Infrastructure.Identity;
using CloudCrafter.UseCases.Contributors.Create;
using CloudCrafter.Web.Infrastructure;
using FastEndpoints;
using FastEndpoints.Swagger;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Extensions.Logging;

var logger = Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

logger.Information("Starting web host");

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration));
var microsoftLogger = new SerilogLoggerFactory(logger)
    .CreateLogger<Program>();

// Configure Web Behavior
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});


builder.Services.AddFastEndpoints()
    .SwaggerDocument(o =>
    {
        o.ShortSchemaNames = true;
    });

ConfigureMediatR();

builder.Services.AddInfrastructureServices(builder.Configuration, microsoftLogger)
    .AddCloudCrafterIdentity(builder.Configuration);

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
if (builder.Environment.IsDevelopment())
{
    // Use a local test email server
    // See: https://ardalis.com/configuring-a-local-test-email-server/
    builder.Services.AddScoped<IEmailSender, MimeKitEmailSender>();

    // Otherwise use this:
    //builder.Services.AddScoped<IEmailSender, FakeEmailSender>();
    AddShowAllServicesSupport();
}
else
{
    builder.Services.AddScoped<IEmailSender, MimeKitEmailSender>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // app.UseDeveloperExceptionPage();
    // app.UseShowAllServicesMiddleware(); // see https://github.com/ardalis/AspNetCoreStartupServices
}
else
{
    //  app.UseDefaultExceptionHandler(); // from FastEndpoints
    app.UseHsts();
}


app.UseFastEndpoints()
    .UseSwaggerGen(); // Includes AddFileServer and static files middleware

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(options => { });
SeedAppDatabase(app);

app.MapEndpoints();

app.Run();

static void SeedAppDatabase(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;

    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        // context.Database.EnsureCreated();
        SeedData.Initialize(services);

        var identityContext = services.GetRequiredService<AppIdentityDbContext>();
        identityContext.Database.Migrate();
        SeedData.InitializeIdentity(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB. {exceptionMessage}", ex.Message);
    }
}

void ConfigureMediatR()
{
    var mediatRAssemblies = new[]
    {
        Assembly.GetAssembly(typeof(Contributor)), // Core
        Assembly.GetAssembly(typeof(CreateContributorCommand)) // UseCases
    };
    builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(mediatRAssemblies!));
    builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    builder.Services.AddScoped<IDomainEventDispatcher, MediatRDomainEventDispatcher>();
}

void AddShowAllServicesSupport()
{
    // add list services for diagnostic purposes - see https://github.com/ardalis/AspNetCoreStartupServices
    builder.Services.Configure<ServiceConfig>(config =>
    {
        config.Services = new List<ServiceDescriptor>(builder.Services);

        // optional - default path to view services is /listallservices - recommended to choose your own path
        config.Path = "/listservices";
    });
}

// Make the implicit Program.cs class public, so integration tests can reference the correct assembly for host building
public partial class Program
{
}
