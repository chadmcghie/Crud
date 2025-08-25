using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Infrastructure.Data;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace Tests.Integration.Backend.Infrastructure;

/// <summary>
/// Base class for integration tests using shared SQL Server with transaction rollback isolation
/// </summary>
public abstract class IntegrationTestBase : IClassFixture<SharedSqlServerWebApplicationFactory>, IDisposable
{
    protected readonly HttpClient Client;
    protected readonly SharedSqlServerWebApplicationFactory Factory;
    protected readonly IServiceScope Scope;
    protected readonly ApplicationDbContext DbContext;

    protected static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected IntegrationTestBase(SharedSqlServerWebApplicationFactory factory)
    {
        Factory = factory;
        
        // Ensure database is created before creating client
        factory.EnsureDatabaseCreated();
        
        Client = factory.CreateClient();
        Scope = factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Ensure database schema is created
        DbContext.Database.EnsureCreated();
    }

    /// <summary>
    /// Clears all data from the database for clean test isolation
    /// This is more reliable than transaction rollback for HTTP-based integration tests
    /// </summary>
    protected async Task RunWithCleanDatabaseAsync(Func<Task> testAction)
    {
        // Clear database before test
        await ClearDatabaseAsync();
        
        try
        {
            await testAction();
        }
        finally
        {
            // Clear database after test for next test
            await ClearDatabaseAsync();
        }
    }

    /// <summary>
    /// Clears all data from the database for clean test isolation
    /// This is more reliable than transaction rollback for HTTP-based integration tests
    /// </summary>
    protected async Task<T> RunWithCleanDatabaseAsync<T>(Func<Task<T>> testAction)
    {
        // Clear database before test
        await ClearDatabaseAsync();
        
        try
        {
            return await testAction();
        }
        finally
        {
            // Clear database after test for next test
            await ClearDatabaseAsync();
        }
    }

    /// <summary>
    /// Legacy method for tests that need database clearing (prefer RunInTransactionAsync)
    /// </summary>
    protected async Task ClearDatabaseAsync()
    {
        // Clear in order to respect foreign key constraints
        DbContext.People.RemoveRange(DbContext.People);
        DbContext.Roles.RemoveRange(DbContext.Roles);
        DbContext.Walls.RemoveRange(DbContext.Walls);
        DbContext.Windows.RemoveRange(DbContext.Windows);
        
        await DbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Helper method to post JSON data
    /// </summary>
    protected async Task<HttpResponseMessage> PostJsonAsync<T>(string requestUri, T data)
    {
        return await Client.PostAsJsonAsync(requestUri, data, JsonOptions);
    }

    /// <summary>
    /// Helper method to put JSON data
    /// </summary>
    protected async Task<HttpResponseMessage> PutJsonAsync<T>(string requestUri, T data)
    {
        return await Client.PutAsJsonAsync(requestUri, data, JsonOptions);
    }

    /// <summary>
    /// Helper method to read JSON response
    /// </summary>
    protected async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content, JsonOptions);
    }

    public void Dispose()
    {
        Scope?.Dispose();
    }
}

/// <summary>
/// Shared SQL Server WebApplicationFactory for integration tests
/// Uses LocalDB or SQL Server Express with unique databases per test class
/// </summary>
public class SharedSqlServerWebApplicationFactory : WebApplicationFactory<Api.Program>
{
    private static readonly object _lockObject = new object();
    private static bool _databaseInitialized = false;
    private readonly string _databaseName;
    private readonly string _connectionString;

    public SharedSqlServerWebApplicationFactory()
    {
        // Each test class gets its own unique database
        _databaseName = $"IntegrationTests_{GetType().Name}_{Guid.NewGuid():N}";
        
        // Use different connection strings for CI vs local development
        var isCI = Environment.GetEnvironmentVariable("CI") == "true";
        if (isCI)
        {
            // CI environment - use SQL Server container
            var ciConnectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING") 
                ?? "Server=localhost,1433;Database=CrudAppTest;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;MultipleActiveResultSets=true;";
            
            // Replace database name with our unique name
            var builder = new SqlConnectionStringBuilder(ciConnectionString)
            {
                InitialCatalog = _databaseName
            };
            _connectionString = builder.ConnectionString;
        }
        else
        {
            // Local development - use LocalDB
            _connectionString = $"Server=(localdb)\\mssqllocaldb;Database={_databaseName};Trusted_Connection=true;MultipleActiveResultSets=true;";
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add SQL Server with our unique database connection string
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseSqlServer(_connectionString);
                options.EnableSensitiveDataLogging();
            });

            // Reduce logging noise in tests
            services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
        });

        builder.UseEnvironment("Testing");
    }

    public void EnsureDatabaseCreated()
    {
        lock (_lockObject)
        {
            if (!_databaseInitialized)
            {
                try
                {
                    // Use appropriate master connection string based on environment
                    var isCI = Environment.GetEnvironmentVariable("CI") == "true";
                    string masterConnectionString;
                    
                    if (isCI)
                    {
                        var ciConnectionString = Environment.GetEnvironmentVariable("SQLSERVER_CONNECTION_STRING") 
                            ?? "Server=localhost,1433;Database=master;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;MultipleActiveResultSets=true;";
                        var builder = new SqlConnectionStringBuilder(ciConnectionString)
                        {
                            InitialCatalog = "master"
                        };
                        masterConnectionString = builder.ConnectionString;
                    }
                    else
                    {
                        masterConnectionString = $"Server=(localdb)\\mssqllocaldb;Database=master;Trusted_Connection=true;";
                    }
                    
                    using var masterConnection = new SqlConnection(masterConnectionString);
                    masterConnection.Open();
                    
                    // Create database if it doesn't exist
                    using var createDbCommand = new SqlCommand($"IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = '{_databaseName}') CREATE DATABASE [{_databaseName}]", masterConnection);
                    createDbCommand.ExecuteNonQuery();
                    
                    _databaseInitialized = true;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(
                        $"Failed to initialize database '{_databaseName}'. " +
                        $"Ensure SQL Server LocalDB is installed and running. " +
                        $"Error: {ex.Message}", ex);
                }
            }
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            try
            {
                // Clean up the test database
                using var scope = Services.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureDeleted();
            }
            catch (Exception)
            {
                // Ignore cleanup errors
            }
        }
        base.Dispose(disposing);
    }
}