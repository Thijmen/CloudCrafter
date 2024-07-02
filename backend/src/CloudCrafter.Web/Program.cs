﻿using System.Reflection;
using CloudCrafter.Core;
using CloudCrafter.Core.Interfaces;
using CloudCrafter.Infrastructure;
using CloudCrafter.Infrastructure.Email;
using CloudCrafter.Web.Infrastructure;
using CloudCrafter.Web.Infrastructure.Swagger;
using Microsoft.OpenApi.Models;
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


builder.Services.AddEndpointsApiExplorer()
    .AddSwaggerGen(swagger =>
    {
        var defaultSchemaIdSelector = swagger.SchemaGeneratorOptions.SchemaIdSelector;
        
        swagger.CustomSchemaIds(type =>
        {
            if (type.MemberType == MemberTypes.NestedType)
            {
                var parentType = type.DeclaringType;
                return parentType!.Name + type.Name;
            }

            return defaultSchemaIdSelector(type);
        });
        swagger.SupportNonNullableReferenceTypes();
        swagger.SchemaFilter<RequireNotNullableSchemaFilter>();
        
        swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            In = ParameterLocation.Header,
            Description = "Please enter a valid token",
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "Bearer"
        });
        
        swagger.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type=ReferenceType.SecurityScheme,
                        Id="Bearer"
                    }
                },
                new string[]{}
            }
        });
    })
    .AddApplicationServices()
    .AddAutoMapper(Assembly.GetExecutingAssembly())
    .AddInfrastructureServices(builder.Configuration, microsoftLogger)
    .AddCloudCrafterIdentity(builder.Configuration)
    .AddCloudCrafterConfiguration(builder.Configuration)
    .AddWebConfig(builder.Configuration);


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
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHsts();
}


app.UseHttpsRedirection();
app.SeedDatabase();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.UseCors("DefaultCorsPolicy");

app.MapEndpoints();

app.Run();

// Make the implicit Program.cs class public, so integration tests can reference the correct assembly for host building
public partial class Program
{
}
