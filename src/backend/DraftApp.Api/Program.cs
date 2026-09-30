using DraftApp.Api;
using DraftApp.Api.Data;
using DraftApp.Api.Data.Repositories;
using DraftApp.Api.Hubs;
using DraftApp.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddOpenApi();

// Add Entity Framework Core
builder.Services.AddDbContext<DraftAppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// Add SignalR
var azureSignalRConnectionString = builder.Configuration.GetConnectionString("AzureSignalR");
if (!string.IsNullOrEmpty(azureSignalRConnectionString))
{
    builder.Services.AddSignalR().AddAzureSignalR(azureSignalRConnectionString);
}
else
{
    builder.Services.AddSignalR();
}

// Add CORS for frontend
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddDraftAppRateLimiting();

// Add repositories
builder.Services.AddScoped<IEventRepository, EventRepository>();

// Add services
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddSingleton<IEventNotificationService, EventNotificationService>();

var app = builder.Build();

// Apply pending migrations on startup. Migrations target SQL Server; tests swap in
// another provider and create the schema themselves.
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<DraftAppDbContext>();
    if (dbContext.Database.IsSqlServer())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Attempting to apply database migrations...");
        dbContext.Database.Migrate();
        logger.LogInformation("Database migrations applied successfully.");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(
        ex,
        "Failed to apply database migrations. Connection string configured: {HasConnectionString}",
        !string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection")));
    throw;
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<EventHub>("/hubs/event");

// Liveness probe used by the container HEALTHCHECK
app.MapMethods("/healthz", new[] { "GET", "HEAD" }, () =>
    Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }));

app.MapFallbackToFile("index.html");

app.Run();

/// <summary>
/// Entry point for the application. Made partial for testing.
/// </summary>
public partial class Program
{
}
