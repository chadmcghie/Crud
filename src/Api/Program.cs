using App;
using Infrastructure;
using FluentValidation;
using FluentValidation.AspNetCore;
using Api.Validators;
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
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddApplication();
            
            // Configure database provider based on configuration
            var databaseProvider = builder.Configuration.GetValue<string>("DatabaseProvider") ?? "SQLite";
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            
            // Support worker-specific databases for E2E test isolation
            var workerDatabase = Environment.GetEnvironmentVariable("WORKER_DATABASE");
            var workerIndex = Environment.GetEnvironmentVariable("WORKER_INDEX");
            
            if (!string.IsNullOrEmpty(workerDatabase) && databaseProvider.ToLowerInvariant() == "sqlite")
            {
                connectionString = $"Data Source={workerDatabase}";
                Console.WriteLine($"🗄️  Using worker-specific database: {workerDatabase}");
            }
            else if (!string.IsNullOrEmpty(workerIndex) && databaseProvider.ToLowerInvariant() == "sqlite")
            {
                // Auto-generate worker-specific database name
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var workerDbName = $"CrudAppTest_Worker{workerIndex}_{timestamp}.db";
                connectionString = $"Data Source={workerDbName}";
                Console.WriteLine($"🗄️  Auto-generated worker database for worker {workerIndex}: {workerDbName}");
            }
            
            // Support tenant-based isolation for E2E tests
            var tenantPrefix = Environment.GetEnvironmentVariable("TENANT_PREFIX");
            if (!string.IsNullOrEmpty(tenantPrefix))
            {
                Console.WriteLine($"🏢 Using tenant-based isolation with prefix: {tenantPrefix}");
            }

            switch (databaseProvider.ToLowerInvariant())
            {
                case "sqlserver":
                    if (string.IsNullOrEmpty(connectionString))
                        throw new InvalidOperationException("Connection string 'DefaultConnection' is required when using SQL Server provider.");
                    builder.Services.AddInfrastructureEntityFrameworkSqlServer(connectionString);
                    break;
                case "sqlite":
                default:
                    if (string.IsNullOrEmpty(connectionString))
                        throw new InvalidOperationException("Connection string 'DefaultConnection' is required when using SQLite provider.");
                    builder.Services.AddInfrastructureEntityFrameworkSqlite(connectionString);
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
            
            // Configure FluentValidation
            builder.Services.AddFluentValidationAutoValidation();
            builder.Services.AddValidatorsFromAssemblyContaining<CreatePersonRequestValidator>();
            
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

            // Ensure database is created for Entity Framework providers
            if (databaseProvider.ToLowerInvariant() is "sqlserver" or "sqlite" or "entityframeworkinmemory")
            {
                await app.Services.EnsureDatabaseAsync();
            }
            
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

            await app.RunAsync();
        }
    }
}
