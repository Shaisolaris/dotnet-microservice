using Microsoft.EntityFrameworkCore;
using ProductService.HealthChecks;
using ProductService.Messaging;
using ProductService.Services;

var builder = WebApplication.CreateBuilder(args);

// ─── Services ───────────────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => c.SwaggerDoc("v1", new() { Title = "Product Microservice", Version = "v1" }));

// EF Core (InMemory for demo, swap to SQL Server/PostgreSQL for production)
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseInMemoryDatabase("ProductDb"));

// Application services
builder.Services.AddScoped<ProductCatalogService>();

// Messaging (use InMemory fallback if RabbitMQ unavailable)
var rabbitHost = builder.Configuration["RabbitMQ:Host"];
if (!string.IsNullOrEmpty(rabbitHost))
{
    try
    {
        builder.Services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
    }
    catch
    {
        builder.Services.AddSingleton<IMessageBus, InMemoryMessageBus>();
    }
}
else
{
    builder.Services.AddSingleton<IMessageBus, InMemoryMessageBus>();
}

// Health checks
var startupCheck = new StartupHealthCheck();
builder.Services.AddSingleton(startupCheck);
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "ready" })
    .AddCheck<StartupHealthCheck>("startup", tags: new[] { "live" });

// CORS
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(b => b.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// ─── Pipeline ───────────────────────────────────────────

var app = builder.Build();

// Ensure database is created and seeded
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

// Health endpoints
app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => true,
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds,
        };
        await context.Response.WriteAsJsonAsync(result);
    }
});

app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
});

app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
});

app.MapControllers();

// Mark as ready
startupCheck.IsReady = true;

app.Run();
