using App;
using Infrastructure;
using Serilog;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api
{
    /// <summary>
    /// DateTime converter that round-trips as UTC and writes "Z".
    /// </summary>
    public sealed class UtcDateTimeConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // System.Text.Json handles ISO 8601 including "Z" correctly here
            var dt = reader.GetDateTime();
            return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        {
            var utc = value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
            writer.WriteStringValue(utc.ToString("O")); // "O" adds Z for UTC
        }
    }

    /// <summary>
    /// Nullable variant of UtcDateTimeConverter.
    /// </summary>
    public sealed class UtcNullableDateTimeConverter : JsonConverter<DateTime?>
    {
        public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null) return null;
            var dt = reader.GetDateTime();
            return dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
        }

        public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            var utc = value.Value.Kind == DateTimeKind.Utc ? value.Value : value.Value.ToUniversalTime();
            writer.WriteStringValue(utc.ToString("O"));
        }
    }

    public class Program
    {
        public static async Task Main(string[] args)
        {
            // 1) Bootstrap Serilog from configuration before building the host
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: true)
                    .AddEnvironmentVariables()
                    .Build())
                .CreateLogger();

            try
            {
                Log.Information("Starting Crud API application");

                var builder = WebApplication.CreateBuilder(args);

                // Replace default logging with Serilog
                builder.Host.UseSerilog();

                // 2) Observability: OpenTelemetry (logs/traces/metrics)
                builder.Services.AddOpenTelemetry()
                    .ConfigureResource(r => r
                        .AddService(serviceName: "Crud.Api", serviceVersion: "1.0.0")
                        .AddAttributes(new Dictionary<string, object>
                        {
                            ["deployment.environment"] = builder.Environment.EnvironmentName
                        }))
                    .WithTracing(t => t
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter())
                    .WithMetrics(m => m
                        .AddAspNetCoreInstrumentation()
                        .AddHttpClientInstrumentation()
                        .AddConsoleExporter());

                // 3) App & Infra
                builder.Services.AddApplication();

                var databaseProvider = (builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SQLite").Trim();
                var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

                // E2E test isolation helpers (only for SQLite)
                var workerDatabase = Environment.GetEnvironmentVariable("WORKER_DATABASE");
                var workerIndex = Environment.GetEnvironmentVariable("WORKER_INDEX");

                if (databaseProvider.Equals("sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    if (!string.IsNullOrWhiteSpace(workerDatabase))
                    {
                        connectionString = $"Data Source={workerDatabase}";
                        Log.Information("🗄️ Using worker-specific SQLite database: {WorkerDatabase}", workerDatabase);
                    }
                    else if (!string.IsNullOrWhiteSpace(workerIndex))
                    {
                        var ts = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        var workerDbName = $"CrudAppTest_Worker{workerIndex}_{ts}.db";
                        connectionString = $"Data Source={workerDbName}";
                        Log.Information("🗄️ Auto-generated worker SQLite database for worker {WorkerIndex}: {WorkerDbName}", workerIndex, workerDbName);
                    }

                    var tenantPrefix = Environment.GetEnvironmentVariable("TENANT_PREFIX");
                    if (!string.IsNullOrWhiteSpace(tenantPrefix))
                    {
                        Log.Information("🏢 Tenant-based isolation prefix: {TenantPrefix}", tenantPrefix);
                    }
                }

                bool usingEfProvider = false;

                switch (databaseProvider.ToLowerInvariant())
                {
                    case "sqlserver":
                        if (string.IsNullOrWhiteSpace(connectionString))
                            throw new InvalidOperationException("Connection string 'DefaultConnection' is required for SQL Server.");
                        builder.Services.AddInfrastructureEntityFrameworkSqlServer(connectionString);
                        Log.Information("Using SQL Server provider");
                        usingEfProvider = true;
                        break;

                    case "sqlite":
                        if (string.IsNullOrWhiteSpace(connectionString))
                            throw new InvalidOperationException("Connection string
