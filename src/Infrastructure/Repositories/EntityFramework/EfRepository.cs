using Ardalis.Specification.EntityFrameworkCore;
using Domain.Interfaces;
using Infrastructure.Data;

namespace Infrastructure.Repositories.EntityFramework;

/// <summary>
/// Generic Entity Framework repository implementation using Ardalis.Specification.EntityFrameworkCore
/// Provides specification pattern support with EF Core integration
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class EfRepository<T> : RepositoryBase<T>, IRepository<T> where T : class
{
    /// <summary>
    /// Initializes a new instance of the EfRepository
    /// </summary>
    /// <param name="dbContext">The Entity Framework DbContext</param>
    public EfRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}