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

        // Use SQLite for migrations - this connection string is only used for design-time operations
        optionsBuilder.UseSqlite("Data Source=CrudAppDesignTime.db");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
