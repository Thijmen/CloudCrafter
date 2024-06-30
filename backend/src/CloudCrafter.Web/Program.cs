﻿using System.Reflection;
using Ardalis.ListStartupServices;
using CloudCrafter.Core;
using CloudCrafter.Core.Common.Interfaces;
using CloudCrafter.Core.ContributorAggregate;
using CloudCrafter.Core.Interfaces;
using CloudCrafter.Infrastructure;
using CloudCrafter.Infrastructure.Core.Configuration;
using CloudCrafter.Infrastructure.Data;
using CloudCrafter.Infrastructure.Email;
using CloudCrafter.Infrastructure.Identity;
using CloudCrafter.Web.Infrastructure;
using CloudCrafter.Web.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Extensions.Logging;

var logger = Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

logger.Information("Starting web host");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSwaggerSetup();
builder.Host.UseSerilog((_, config) => config.ReadFrom.Configuration(builder.Configuration));
var microsoftLogger = new SerilogLoggerFactory(logger)
    .CreateLogger<Program>();

// Configure Web Behavior
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

ConfigureMediatR();
ConfigureAutoMapper();

builder.Services.AddInfrastructureServices(builder.Configuration, microsoftLogger)
    .AddCloudCrafterIdentity(builder.Configuration)
    .AddCloudCrafterConfiguration(builder.Configuration);

builder.Services.AddCors(options =>
{
    var corsSettings = new CorsSettings();
    builder.Configuration.Bind(CorsSettings.KEY, corsSettings);
    
    options.AddPolicy("DefaultCorsPolicy", corsBuilder =>
    {
        corsBuilder.WithOrigins(corsSettings.AllowedOrigins.ToArray())
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddExceptionHandler<CustomExceptionHandler>();
builder.Services.AddScoped<IUser, CurrentUser>();
if (builder.Environment.IsDevelopment())
{
    // Use a local test email server
    // See: https://ardalis.com/configuring-a-local-test-email-server/
    builder.Services.AddScoped<IEmailSender, MimeKitEmailSender>();
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



app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseExceptionHandler(options => { });
SeedAppDatabase(app);


app.UseSwaggerSetup();
app.MapEndpoints();

app.MapGet("/greeting", () => "Hello, world")
    .WithGroupName("v1");

app.UseCors("DefaultCorsPolicy");

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
    };
    builder.Services.AddApplicationServices(mediatRAssemblies);
}

void ConfigureAutoMapper()
{
    builder.Services.AddAutoMapper(Assembly.GetExecutingAssembly());
}




// Make the implicit Program.cs class public, so integration tests can reference the correct assembly for host building
public partial class Program
{
}
