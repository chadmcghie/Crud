using App;
using Infrastructure;
using Serilog;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using static System.Net.Mime.MediaTypeNames;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api
{
    /// <summary>
    /// Custom DateTime converter that ensures UTC DateTime values are serialized with "Z" suffix
    /// </summary>
    public class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return DateTime.Parse(reader.GetString()!);
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            // Ensure the DateTime is treated as UTC and formatted with "Z" suffix
            var utcValue = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
            writer.WriteStringValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
        }
    }

    /// <summary>
    /// Custom nullable DateTime converter that ensures UTC DateTime values are serialized with "Z" suffix
    /// </summary>
    public class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var stringValue = reader.GetString();
            return string.IsNullOrEmpty(stringValue) ? null : DateTime.Parse(stringValue);
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (value.HasValue)
            {
                // Ensure the DateTime is treated as UTC and formatted with "Z" suffix
                var utcValue = value.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value.Value.ToUniversalTime();
                writer.WriteStringValue(utcValue.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
            }
            else
            {
                writer.WriteNullValue();
            }
        }
    }
    
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // Configure Serilog first
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json")
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .Build())
                .CreateLogger();

            try
            {
                Log.Information("Starting Crud API application");

                var builder = WebApplication.CreateBuilder(args);

                // Replace default logging with Serilog
                builder.Host.UseSerilog();

                // Configure OpenTelemetry
                builder.Services.AddOpenTelemetry()
                    .ConfigureResource(resource => resource
                        .AddService("Crud.Api", "1.0.0")
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = builder.Environment.EnvironmentName
                        }))
                    .WithTracing(tracing => tracing
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter())
                    .WithMetrics(metrics => metrics
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter());

                builder.Services.AddApplication();
                
                // Configure database provider based on configuration
                var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "InMemory";
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
                
                // Support worker-specific databases for E2E test isolation
                var workerDatabase = Environment.GetEnvironmentVariable("WORKER_DATABASE");
                var workerIndex = Environment.GetEnvironmentVariable("WORKER_INDEX");
                
                if (!string.IsNullOrEmpty(workerDatabase) && databaseProvider.ToLowerInvariant() == "sqlite")
                {
                    connectionString = $"Data Source={workerDatabase}";
                    Log.Information("🗄️  Using worker-specific database: {WorkerDatabase}", workerDatabase);
                }
                else if (!string.IsNullOrEmpty(workerIndex) && databaseProvider.ToLowerInvariant() == "sqlite")
                {
                    // Auto-generate worker-specific database name
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var workerDbName = $"CrudAppTest_Worker{workerIndex}_{timestamp}.db";
                    connectionString = $"Data Source={workerDbName}";
                    Log.Information("🗄️  Auto-generated worker database for worker {WorkerIndex}: {WorkerDbName}", workerIndex, workerDbName);
                }
                
                // Support tenant-based isolation for E2E tests
                var tenantPrefix = Environment.GetEnvironmentVariable("TENANT_PREFIX");
                if (!string.IsNullOrEmpty(tenantPrefix))
                {
                    Log.Information("🏢 Using tenant-based isolation with prefix: {TenantPrefix}", tenantPrefix);
                }

                // For now, use InMemory until Entity Framework infrastructure is available
                switch (databaseProvider.ToLowerInvariant())
                {
                    case "sqlserver":
                        Log.Warning("SQL Server provider requested but not available, falling back to InMemory");
                        builder.Services.AddInfrastructureInMemory();
                        break;
                    case "sqlite":
                        Log.Warning("SQLite provider requested but not available, falling back to InMemory");
                        builder.Services.AddInfrastructureInMemory();
                        break;
                    case "inmemory":
                    default:
                        Log.Information("Using InMemory database provider");
                        builder.Services.AddInfrastructureInMemory();
                        break;
                }

                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAngular", policy =>
                    {
                        policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200", "https://localhost:4200")
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
                });
                builder.Services.AddControllers()
                    .AddJsonOptions(options =>
                    {
                        // Configure DateTime serialization to include UTC "Z" suffix
                        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                        // Configure property naming to camelCase
                        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                        // Add custom DateTime converters to ensure UTC "Z" suffix
                        options.JsonSerializerOptions.Converters.Add(new UtcDateTimeConverter());
                        options.JsonSerializerOptions.Converters.Add(new UtcNullableDateTimeConverter());
                    });            
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen();
                builder.Services.AddHealthChecks();
                
                builder.Services.AddMediatR(services => services.RegisterServicesFromAssembly(typeof(Program).Assembly));            
                builder.Services.AddAutoMapper(
                    cfg => { },
                    typeof(App.DependencyInjection).Assembly, 
                    typeof(Infrastructure.DependencyInjection).Assembly
                    );

                var app = builder.Build();
                
                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }

                app.UseHttpsRedirection();
                app.UseCors("AllowAngular");
                app.UseAuthorization();
                app.MapControllers();
                app.MapHealthChecks("/health");

                Log.Information("Crud API application configured successfully");
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
