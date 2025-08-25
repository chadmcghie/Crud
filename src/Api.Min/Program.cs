using Api.Dto;
using App;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddApplication();

// Configure database provider based on configuration
var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "InMemory";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

switch (databaseProvider.ToLowerInvariant())
{
    case "sqlserver":
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' is required when using SQL Server provider.");
        builder.Services.AddInfrastructureEntityFrameworkSqlServer(connectionString);
        break;
    case "entityframeworkinmemory":
        builder.Services.AddInfrastructureEntityFrameworkInMemory();
        break;
    case "inmemory":
    default:
        builder.Services.AddInfrastructureInMemory();
        break;
}

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Ensure database is created for Entity Framework providers
if (databaseProvider.ToLowerInvariant() is "sqlserver" or "entityframeworkinmemory")
{
    await app.Services.EnsureDatabaseAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast")
.WithOpenApi();


var demoEndpoints = new Api.Endpoints.UserEndpointsDemo();
demoEndpoints.MapEndpoints(app);


app.Run();
