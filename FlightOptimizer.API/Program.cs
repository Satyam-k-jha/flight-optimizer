using FlightOptimizer.Core.Interfaces;
using FlightOptimizer.Infrastructure.Data;
using FlightOptimizer.Infrastructure.Repositories;
using FlightOptimizer.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DB Config
var dbProvider = builder.Configuration.GetValue<string>("DbProvider");
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (dbProvider == "PostgreSQL")
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString,
            x => x.UseNetTopologySuite()));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, 
            x => x.UseNetTopologySuite()));
}

// Repositories
builder.Services.AddScoped<IAirportRepository, AirportRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IRestrictedZoneRepository, RestrictedZoneRepository>();
builder.Services.AddTransient<DataSeeder>();

// Graph Engine (Singleton)
builder.Services.AddSingleton<IGraphEngine, GraphEngine>();

// CORS
    // Fix CORS: Allow Angular
    builder.Services.AddCors(options => options.AddPolicy("AllowAngular",
        policy => policy.WithOrigins("http://localhost:4200")
                        .AllowAnyMethod()
                        .AllowAnyHeader()));

var app = builder.Build();

// Seed Data & Initialize Graph
// Seed Data & Initialize Graph
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try 
    {
        var seeder = services.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }

    var graphEngine = services.GetRequiredService<IGraphEngine>() as GraphEngine;
    var airportRepo = services.GetRequiredService<IAirportRepository>();
    var routeRepo = services.GetRequiredService<IRouteRepository>();
    var zoneRepo = services.GetRequiredService<IRestrictedZoneRepository>();

    var airports = await airportRepo.GetAllAsync();
    var routes = await routeRepo.GetAllAsync();
    var zones = await zoneRepo.GetAllAsync();
    
    graphEngine?.Initialize(airports, routes, zones);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAngular");
app.UseAuthorization();
app.MapControllers();

app.Run();
