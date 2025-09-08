using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using App.Abstractions;
using Domain.Entities;

namespace Infrastructure.Services.Caching;

/// <summary>
/// Generic decorator for repository caching that wraps any repository interface
/// and provides transparent caching for read operations with cache invalidation
/// on write operations.
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TRepository">The repository interface type</typeparam>
public class CachedRepositoryDecorator<TEntity, TRepository> : IRepository<TEntity>
    where TEntity : class
    where TRepository : IRepository<TEntity>
{
    private readonly TRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ICacheConfiguration _cacheConfig;

    public CachedRepositoryDecorator(
        TRepository repository,
        ICacheService cacheService,
        ICacheKeyGenerator keyGenerator,
        ICacheConfiguration cacheConfig)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        _keyGenerator = keyGenerator ?? throw new ArgumentNullException(nameof(keyGenerator));
        _cacheConfig = cacheConfig ?? throw new ArgumentNullException(nameof(cacheConfig));
    }

    public async Task<TEntity?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<TEntity>(id);

        var cachedEntity = await _cacheService.GetAsync<TEntity>(cacheKey, cancellationToken);
        if (cachedEntity != null)
        {
            return cachedEntity;
        }

        var entity = await _repository.GetAsync(id, cancellationToken);
        if (entity != null)
        {
            var options = _cacheConfig.GetCacheOptions<TEntity>();
            await _cacheService.SetAsync(cacheKey, entity, options, cancellationToken);
        }

        return entity;
    }

    public async Task<IReadOnlyList<TEntity>> ListAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateListKey<TEntity>();

        var cachedList = await _cacheService.GetAsync<IReadOnlyList<TEntity>>(cacheKey, cancellationToken);
        if (cachedList != null)
        {
            return cachedList;
        }

        var entities = await _repository.ListAsync(cancellationToken);
        if (entities.Any())
        {
            var options = _cacheConfig.GetCacheOptions<TEntity>();
            await _cacheService.SetAsync(cacheKey, entities, options, cancellationToken);
        }

        return entities;
    }

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var result = await _repository.AddAsync(entity, cancellationToken);

        // Invalidate list cache since we added a new entity
        await InvalidateListCache<TEntity>(cancellationToken);

        return result;
    }

    public async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(entity, cancellationToken);

        // Invalidate both entity and list caches
        var id = GetEntityId(entity);
        if (id.HasValue)
        {
            await InvalidateEntityCache<TEntity>(id.Value, cancellationToken);
        }
        await InvalidateListCache<TEntity>(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);

        // Invalidate both entity and list caches
        await InvalidateEntityCache<TEntity>(id, cancellationToken);
        await InvalidateListCache<TEntity>(cancellationToken);
    }

    private async Task InvalidateEntityCache<T>(Guid id, CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<T>(id);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private async Task InvalidateListCache<T>(CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateListKey<T>();
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private Guid? GetEntityId(TEntity entity)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty != null && idProperty.GetValue(entity) is Guid id)
        {
            return id;
        }
        return null;
    }

    protected TRepository Repository => _repository;
}

/// <summary>
/// Specialized decorator for PersonRepository
/// </summary>
public class CachedPersonRepositoryDecorator : IPersonRepository
{
    private readonly IPersonRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ICacheConfiguration _cacheConfig;

    public CachedPersonRepositoryDecorator(IPersonRepository repository, ICacheService cacheService, ICacheKeyGenerator keyGenerator, ICacheConfiguration cacheConfig)
    {
        _repository = repository;
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
        _cacheConfig = cacheConfig;
    }

    public async Task<Person?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<Person>(id);
        var cachedEntity = await _cacheService.GetAsync<Person>(cacheKey, cancellationToken);
        if (cachedEntity != null)
        {
            return cachedEntity;
        }

        var entity = await _repository.GetAsync(id, cancellationToken);
        if (entity != null)
        {
            var options = _cacheConfig.GetCacheOptions<Person>();
            await _cacheService.SetAsync(cacheKey, entity, options, cancellationToken);
        }
        return entity;
    }

    public async Task<IReadOnlyList<Person>> ListAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateListKey<Person>();
        var cachedList = await _cacheService.GetAsync<IReadOnlyList<Person>>(cacheKey, cancellationToken);
        if (cachedList != null)
        {
            return cachedList;
        }

        var list = await _repository.ListAsync(cancellationToken);
        var options = _cacheConfig.GetCacheOptions<Person>();
        await _cacheService.SetAsync(cacheKey, list, options, cancellationToken);
        return list;
    }

    public async Task<Person> AddAsync(Person entity, CancellationToken cancellationToken = default)
    {
        var result = await _repository.AddAsync(entity, cancellationToken);
        var id = GetEntityId(result);
        if (id.HasValue)
        {
            await InvalidateEntityCache<Person>(id.Value, cancellationToken);
        }
        await InvalidateListCache<Person>(cancellationToken);
        return result;
    }

    public async Task UpdateAsync(Person entity, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(entity, cancellationToken);
        var id = GetEntityId(entity);
        if (id.HasValue)
        {
            await InvalidateEntityCache<Person>(id.Value, cancellationToken);
        }
        await InvalidateListCache<Person>(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
        await InvalidateEntityCache<Person>(id, cancellationToken);
        await InvalidateListCache<Person>(cancellationToken);
    }

    private async Task InvalidateEntityCache<T>(Guid id, CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<T>(id);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private async Task InvalidateListCache<T>(CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateListKey<T>();
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private Guid? GetEntityId(Person entity)
    {
        var idProperty = typeof(Person).GetProperty("Id");
        if (idProperty != null && idProperty.GetValue(entity) is Guid id)
        {
            return id;
        }
        return null;
    }
}

/// <summary>
/// Specialized decorator for RoleRepository that handles the GetByNameAsync method
/// </summary>
public class CachedRoleRepositoryDecorator : IRoleRepository
{
    private readonly IRoleRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ICacheConfiguration _cacheConfig;

    public CachedRoleRepositoryDecorator(IRoleRepository repository, ICacheService cacheService, ICacheKeyGenerator keyGenerator, ICacheConfiguration cacheConfig)
    {
        _repository = repository;
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
        _cacheConfig = cacheConfig;
    }

    public async Task<Role?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<Role>(id);
        var cachedEntity = await _cacheService.GetAsync<Role>(cacheKey, cancellationToken);
        if (cachedEntity != null)
        {
            return cachedEntity;
        }

        var entity = await _repository.GetAsync(id, cancellationToken);
        if (entity != null)
        {
            var options = _cacheConfig.GetCacheOptions<Role>();
            await _cacheService.SetAsync(cacheKey, entity, options, cancellationToken);
        }
        return entity;
    }

    public async Task<IReadOnlyList<Role>> ListAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateListKey<Role>();
        var cachedList = await _cacheService.GetAsync<IReadOnlyList<Role>>(cacheKey, cancellationToken);
        if (cachedList != null)
        {
            return cachedList;
        }

        var list = await _repository.ListAsync(cancellationToken);
        var options = _cacheConfig.GetCacheOptions<Role>();
        await _cacheService.SetAsync(cacheKey, list, options, cancellationToken);
        return list;
    }

    public async Task<Role> AddAsync(Role entity, CancellationToken cancellationToken = default)
    {
        var result = await _repository.AddAsync(entity, cancellationToken);
        var id = GetEntityId(result);
        if (id.HasValue)
        {
            await InvalidateEntityCache<Role>(id.Value, cancellationToken);
        }
        await InvalidateListCache<Role>(cancellationToken);
        return result;
    }

    public async Task UpdateAsync(Role entity, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(entity, cancellationToken);
        var id = GetEntityId(entity);
        if (id.HasValue)
        {
            await InvalidateEntityCache<Role>(id.Value, cancellationToken);
        }
        await InvalidateListCache<Role>(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
        await InvalidateEntityCache<Role>(id, cancellationToken);
        await InvalidateListCache<Role>(cancellationToken);
    }

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateNameKey<Role>(name);

        var cachedRole = await _cacheService.GetAsync<Role>(cacheKey, cancellationToken);
        if (cachedRole != null)
        {
            return cachedRole;
        }

        var role = await _repository.GetByNameAsync(name, cancellationToken);
        if (role != null)
        {
            var options = _cacheConfig.GetCacheOptions<Role>();
            await _cacheService.SetAsync(cacheKey, role, options, cancellationToken);
        }

        return role;
    }

    private async Task InvalidateEntityCache<T>(Guid id, CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<T>(id);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private async Task InvalidateListCache<T>(CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateListKey<T>();
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private Guid? GetEntityId(Role entity)
    {
        var idProperty = typeof(Role).GetProperty("Id");
        if (idProperty != null && idProperty.GetValue(entity) is Guid id)
        {
            return id;
        }
        return null;
    }
}

/// <summary>
/// Specialized decorator for WallRepository
/// </summary>
public class CachedWallRepositoryDecorator : IWallRepository
{
    private readonly IWallRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ICacheConfiguration _cacheConfig;

    public CachedWallRepositoryDecorator(IWallRepository repository, ICacheService cacheService, ICacheKeyGenerator keyGenerator, ICacheConfiguration cacheConfig)
    {
        _repository = repository;
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
        _cacheConfig = cacheConfig;
    }

    public async Task<Wall?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<Wall>(id);
        var cachedEntity = await _cacheService.GetAsync<Wall>(cacheKey, cancellationToken);
        if (cachedEntity != null)
        {
            return cachedEntity;
        }

        var entity = await _repository.GetAsync(id, cancellationToken);
        if (entity != null)
        {
            var options = _cacheConfig.GetCacheOptions<Wall>();
            await _cacheService.SetAsync(cacheKey, entity, options, cancellationToken);
        }
        return entity;
    }

    public async Task<IReadOnlyList<Wall>> ListAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateListKey<Wall>();
        var cachedList = await _cacheService.GetAsync<IReadOnlyList<Wall>>(cacheKey, cancellationToken);
        if (cachedList != null)
        {
            return cachedList;
        }

        var list = await _repository.ListAsync(cancellationToken);
        var options = _cacheConfig.GetCacheOptions<Wall>();
        await _cacheService.SetAsync(cacheKey, list, options, cancellationToken);
        return list;
    }

    public async Task<Wall> AddAsync(Wall entity, CancellationToken cancellationToken = default)
    {
        var result = await _repository.AddAsync(entity, cancellationToken);
        var id = GetEntityId(result);
        if (id.HasValue)
        {
            await InvalidateEntityCache<Wall>(id.Value, cancellationToken);
        }
        await InvalidateListCache<Wall>(cancellationToken);
        return result;
    }

    public async Task UpdateAsync(Wall entity, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(entity, cancellationToken);
        var id = GetEntityId(entity);
        if (id.HasValue)
        {
            await InvalidateEntityCache<Wall>(id.Value, cancellationToken);
        }
        await InvalidateListCache<Wall>(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
        await InvalidateEntityCache<Wall>(id, cancellationToken);
        await InvalidateListCache<Wall>(cancellationToken);
    }

    private async Task InvalidateEntityCache<T>(Guid id, CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<T>(id);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private async Task InvalidateListCache<T>(CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateListKey<T>();
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private Guid? GetEntityId(Wall entity)
    {
        var idProperty = typeof(Wall).GetProperty("Id");
        if (idProperty != null && idProperty.GetValue(entity) is Guid id)
        {
            return id;
        }
        return null;
    }
}

/// <summary>
/// Specialized decorator for WindowRepository
/// </summary>
public class CachedWindowRepositoryDecorator : IWindowRepository
{
    private readonly IWindowRepository _repository;
    private readonly ICacheService _cacheService;
    private readonly ICacheKeyGenerator _keyGenerator;
    private readonly ICacheConfiguration _cacheConfig;

    public CachedWindowRepositoryDecorator(IWindowRepository repository, ICacheService cacheService, ICacheKeyGenerator keyGenerator, ICacheConfiguration cacheConfig)
    {
        _repository = repository;
        _cacheService = cacheService;
        _keyGenerator = keyGenerator;
        _cacheConfig = cacheConfig;
    }

    public async Task<Window?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<Window>(id);
        var cachedEntity = await _cacheService.GetAsync<Window>(cacheKey, cancellationToken);
        if (cachedEntity != null)
        {
            return cachedEntity;
        }

        var entity = await _repository.GetAsync(id, cancellationToken);
        if (entity != null)
        {
            var options = _cacheConfig.GetCacheOptions<Window>();
            await _cacheService.SetAsync(cacheKey, entity, options, cancellationToken);
        }
        return entity;
    }

    public async Task<IReadOnlyList<Window>> ListAsync(CancellationToken cancellationToken = default)
    {
        var cacheKey = _keyGenerator.GenerateListKey<Window>();
        var cachedList = await _cacheService.GetAsync<IReadOnlyList<Window>>(cacheKey, cancellationToken);
        if (cachedList != null)
        {
            return cachedList;
        }

        var list = await _repository.ListAsync(cancellationToken);
        var options = _cacheConfig.GetCacheOptions<Window>();
        await _cacheService.SetAsync(cacheKey, list, options, cancellationToken);
        return list;
    }

    public async Task<Window> AddAsync(Window entity, CancellationToken cancellationToken = default)
    {
        var result = await _repository.AddAsync(entity, cancellationToken);
        var id = GetEntityId(result);
        if (id.HasValue)
        {
            await InvalidateEntityCache<Window>(id.Value, cancellationToken);
        }
        await InvalidateListCache<Window>(cancellationToken);
        return result;
    }

    public async Task UpdateAsync(Window entity, CancellationToken cancellationToken = default)
    {
        await _repository.UpdateAsync(entity, cancellationToken);
        var id = GetEntityId(entity);
        if (id.HasValue)
        {
            await InvalidateEntityCache<Window>(id.Value, cancellationToken);
        }
        await InvalidateListCache<Window>(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteAsync(id, cancellationToken);
        await InvalidateEntityCache<Window>(id, cancellationToken);
        await InvalidateListCache<Window>(cancellationToken);
    }

    private async Task InvalidateEntityCache<T>(Guid id, CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateEntityKey<T>(id);
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private async Task InvalidateListCache<T>(CancellationToken cancellationToken) where T : class
    {
        var cacheKey = _keyGenerator.GenerateListKey<T>();
        await _cacheService.RemoveAsync(cacheKey, cancellationToken);
    }

    private Guid? GetEntityId(Window entity)
    {
        var idProperty = typeof(Window).GetProperty("Id");
        if (idProperty != null && idProperty.GetValue(entity) is Guid id)
        {
            return id;
        }
        return null;
    }
}

