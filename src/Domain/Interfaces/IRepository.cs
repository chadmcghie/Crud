using Ardalis.Specification;

namespace Domain.Interfaces;

/// <summary>
/// Generic repository interface extending Ardalis.Specification's IRepositoryBase
/// Provides standard CRUD operations with specification pattern support
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> : IRepositoryBase<T> where T : class
{
    // The base interface already provides all needed methods:
    // - GetByIdAsync, GetBySpecAsync, ListAsync, SingleOrDefaultAsync, FirstOrDefaultAsync
    // - AddAsync, UpdateAsync, DeleteAsync, DeleteRangeAsync
    // - CountAsync, AnyAsync
    // - All methods support specifications for complex queries
    
    // Add any additional domain-specific methods here if needed
}