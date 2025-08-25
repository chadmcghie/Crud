using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Infrastructure.Data;

/// <summary>
/// Design-time factory for ApplicationDbContext used by Entity Framework tools
/// This enables migrations and other design-time operations
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use SQL Server for migrations - this connection string is only used for design-time operations
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=CrudAppDesignTime;Trusted_Connection=true;MultipleActiveResultSets=true");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}