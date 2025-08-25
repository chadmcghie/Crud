using App;
using Infrastructure;
using static System.Net.Mime.MediaTypeNames;

namespace Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

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
                case "sqlite":
                    if (string.IsNullOrEmpty(connectionString))
                        throw new InvalidOperationException("Connection string 'DefaultConnection' is required when using SQLite provider.");
                    builder.Services.AddInfrastructureEntityFrameworkSqlite(connectionString);
                    break;
                case "entityframeworkinmemory":
                    builder.Services.AddInfrastructureEntityFrameworkInMemory();
                    break;
                case "inmemory":
                default:
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
            builder.Services.AddControllers();            
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            
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

            await app.RunAsync();
        }
    }
}
