using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Middleware;
using App;
using FluentValidation;
using Infrastructure;
using Infrastructure.Resilience;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Serilog;


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
            if (reader.TokenType == JsonTokenType.Null)
                return null;
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

                builder.Services.AddHttpClient("default")
                    .AddPolicyHandler((sp, request) => PollyPolicies.GetComprehensiveHttpPolicy(sp));

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
                            throw new InvalidOperationException("Connection string 'DefaultConnection' is required for SQLite.");
                        builder.Services.AddInfrastructureEntityFrameworkSqlite(connectionString);
                        Log.Information("Using SQLite provider with connection: {ConnectionString}", connectionString);
                        usingEfProvider = true;
                        break;

                    default:
                        throw new InvalidOperationException($"Unsupported database provider: {databaseProvider}");
                }

                // 4) CORS, Controllers, and other services
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("AllowAngular", policy =>
                    {
                        policy.WithOrigins(
                                "http://localhost:4200", "http://127.0.0.1:4200", "https://localhost:4200",
                                "http://localhost:4210", "http://localhost:4220", "http://localhost:4230",
                                "http://localhost:4240", "http://localhost:4250", "http://localhost:4260"
                              )
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
                });

                // Add FluentValidation validators
                builder.Services.AddValidatorsFromAssemblyContaining<Program>();
                builder.Services.AddValidatorsFromAssembly(typeof(App.DependencyInjection).Assembly);

                // Configure JWT Authentication
                var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"];
                var jwtIssuer = builder.Configuration["Jwt:Issuer"];
                var jwtAudience = builder.Configuration["Jwt:Audience"];

                if (string.IsNullOrEmpty(jwtSecret))
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        Log.Warning("JWT Secret not configured. Using generated development key - this should not happen in production.");
                        // Generate a secure random key for development if nothing is configured
                        var keyBytes = new byte[64]; // 512 bits
                        using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
                        {
                            rng.GetBytes(keyBytes);
                        }
                        jwtSecret = Convert.ToBase64String(keyBytes);
                    }
                    else
                    {
                        throw new InvalidOperationException("JWT Secret must be configured via JWT_SECRET environment variable or Jwt:Secret configuration for production environments.");
                    }
                }

                // Validate JWT secret strength
                if (jwtSecret.Length < 32)
                {
                    throw new InvalidOperationException("JWT Secret must be at least 32 characters long for security.");
                }

                if (string.IsNullOrEmpty(jwtIssuer))
                {
                    jwtIssuer = "CrudApi";
                }

                if (string.IsNullOrEmpty(jwtAudience))
                {
                    jwtAudience = "CrudApiUsers";
                }

                builder.Services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtIssuer,
                        ValidAudience = jwtAudience,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                        ClockSkew = TimeSpan.Zero
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // Allow the token to be read from cookies as well
                            var token = context.Request.Cookies["accessToken"];
                            if (!string.IsNullOrEmpty(token))
                            {
                                context.Token = token;
                            }
                            return Task.CompletedTask;
                        }
                    };
                });

                // Add authorization policies
                builder.Services.AddAuthorization(options =>
                {
                    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
                    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("User", "Admin"));
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

                // Configure Swagger with JWT support
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo
                    {
                        Title = "Crud API",
                        Version = "v1",
                        Description = "A CRUD API with JWT authentication"
                    });

                    // Add JWT Authentication
                    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                    {
                        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                        Name = "Authorization",
                        In = ParameterLocation.Header,
                        Type = SecuritySchemeType.ApiKey,
                        Scheme = "Bearer"
                    });

                    c.AddSecurityRequirement(new OpenApiSecurityRequirement
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Type = ReferenceType.SecurityScheme,
                                    Id = "Bearer"
                                }
                            },
                            Array.Empty<string>()
                        }
                    });
                });

                // Add rate limiting for authentication endpoints
                builder.Services.AddMemoryCache();
                builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

                builder.Services.AddHealthChecks();

                builder.Services.AddAutoMapper(
                    cfg => { },
                    typeof(Program).Assembly,
                    typeof(App.DependencyInjection).Assembly,
                    typeof(Infrastructure.DependencyInjection).Assembly
                );

                var app = builder.Build();

                // Ensure database is created for Entity Framework providers
                if (usingEfProvider)
                {
                    await app.Services.EnsureDatabaseAsync();
                    Log.Information("Database ensured successfully");
                }

                if (app.Environment.IsDevelopment())
                {
                    app.UseSwagger();
                    app.UseSwaggerUI();
                    Log.Information("Swagger UI enabled for development environment");
                }

                // Security headers middleware
                app.Use(async (context, next) =>
                {
                    // Add security headers using indexer to avoid duplicate key issues
                    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
                    context.Response.Headers["X-Frame-Options"] = "DENY";
                    context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
                    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
                    
                    // Only add HSTS in production with HTTPS
                    if (!app.Environment.IsDevelopment() && context.Request.IsHttps)
                    {
                        context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
                    }

                    // Add CSP header - adjust as needed for your application
                    context.Response.Headers["Content-Security-Policy"] = 
                        "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'";

                    await next();
                });

                app.UseHttpsRedirection();
                app.UseMiddleware<RateLimitingMiddleware>();
                app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
                app.UseCors("AllowAngular");
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapControllers();
                app.MapHealthChecks("/health");

                Log.Information("Application configured successfully. Starting web host...");
                await app.RunAsync();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application terminated unexpectedly");
                throw;
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
