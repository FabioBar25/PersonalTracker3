using k8s.KubeConfigModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PersonalTracker.Api.Managers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

// Add AspireExtensions
builder.AddServiceDefaults();

// Add Azure Monitor for Telemetry
builder.Services.AddOpenTelemetry().UseAzureMonitor();

// Bind Configurations
var environment = builder.Environment;

var mainConfiguration = HostConfiguration.Configure(
    builder.Services,
    builder.Configuration,
    environment.EnvironmentName);


// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Exception Handling
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions.Add("instance", $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}");
    };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddIdentityApiEndpoints<User>()
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<DbContext>();

var app = builder.Build();

//app.MapIdentityApi<User>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    _ = app.MapOpenApi();
    _ = app.MapScalarApiReference();

    // Retrieve an instance of the DbContext class and manually run migrations during startup
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<PpyDbContext>();
    _ = context.Database.EnsureDeleted();
    _ = context.Database.EnsureCreated();
    //context.Database.Migrate();

    DataSeeder.Seed(scope.ServiceProvider).GetAwaiter().GetResult();
}

app.UseHttpsRedirection();
app.UseExceptionHandler();

app.UseAuthorization();

app.MapControllers();

app.Run();

builder.Services.AddScoped<TaskAccessor>();
builder.Services.AddScoped<TaskManager>();

builder.Services.AddScoped<AuthManager>();
