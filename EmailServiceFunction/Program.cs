using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
// builder.Services
//     .AddApplicationInsightsTelemetryWorkerService()
//     .ConfigureFunctionsApplicationInsights();

// Load configuration
builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables();

if (!builder.Environment.IsDevelopment())
{
    // Load secrets from environment variables (set by GitHub Actions)
    builder.Configuration["Values:SendGridApiKey"] = Environment.GetEnvironmentVariable("SENDGRID_API_KEY");
    builder.Configuration["Values:SendGridFromEmail"] = Environment.GetEnvironmentVariable("SENDGRID_FROMEMAIL");
}

builder.Build().Run();
