using App.Abstractions;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Services.Caching;
using Moq;
using Xunit;

namespace Tests.Unit.Backend.Infrastructure.Services;

public class CachedRepositoryDecoratorTests
{
    private readonly Mock<IRoleRepository> _mockRepository;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ICacheKeyGenerator> _mockKeyGenerator;
    private readonly Mock<ICacheConfiguration> _mockCacheConfig;
    private readonly CachedRoleRepositoryDecorator _decorator;

    public CachedRepositoryDecoratorTests()
    {
        _mockRepository = new Mock<IRoleRepository>();
        _mockCacheService = new Mock<ICacheService>();
        _mockKeyGenerator = new Mock<ICacheKeyGenerator>();
        _mockCacheConfig = new Mock<ICacheConfiguration>();
        
        // Setup default cache key generation
        _mockKeyGenerator
            .Setup(x => x.GenerateEntityKey<Role>(It.IsAny<Guid>()))
            .Returns<Guid>(id => $"role:{id}");
        _mockKeyGenerator
            .Setup(x => x.GenerateListKey<Role>())
            .Returns("role:list");
        _mockKeyGenerator
            .Setup(x => x.GenerateNameKey<Role>(It.IsAny<string>()))
            .Returns<string>(name => $"role:name:{name}");
        
        // Setup default cache options
        _mockCacheConfig
            .Setup(x => x.GetCacheOptions<Role>())
            .Returns(new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(15) });
        
        _decorator = new CachedRoleRepositoryDecorator(
            _mockRepository.Object, 
            _mockCacheService.Object,
            _mockKeyGenerator.Object,
            _mockCacheConfig.Object);
    }

    [Fact]
    public async Task GetAsync_WhenCacheHit_ShouldReturnCachedEntity()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cachedRole = new Role { Id = id, Name = "Admin" };
        var cacheKey = $"role:{id}";

        _mockKeyGenerator
            .Setup(x => x.GenerateEntityKey<Role>(id))
            .Returns(cacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedRole);

        // Act
        var result = await _decorator.GetAsync(id);

        // Assert
        result.Should().Be(cachedRole);
        _mockRepository.Verify(x => x.GetAsync(id, It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheService.Verify(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenCacheMiss_ShouldFetchFromRepositoryAndCache()
    {
        // Arrange
        var id = Guid.NewGuid();
        var role = new Role { Id = id, Name = "Admin" };
        var cacheKey = $"role:{id}";

        _mockKeyGenerator
            .Setup(x => x.GenerateEntityKey<Role>(id))
            .Returns(cacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        _mockRepository
            .Setup(x => x.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        // Act
        var result = await _decorator.GetAsync(id);

        // Assert
        result.Should().Be(role);
        _mockRepository.Verify(x => x.GetAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(cacheKey, role, It.IsAny<CacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenRepositoryReturnsNull_ShouldNotCache()
    {
        // Arrange
        var id = Guid.NewGuid();
        var cacheKey = $"role:{id}";

        _mockKeyGenerator
            .Setup(x => x.GenerateEntityKey<Role>(id))
            .Returns(cacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        _mockRepository
            .Setup(x => x.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act
        var result = await _decorator.GetAsync(id);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(x => x.GetAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Role>(), It.IsAny<CacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_WhenCacheHit_ShouldReturnCachedList()
    {
        // Arrange
        var roles = new List<Role>
        {
            new() { Id = Guid.NewGuid(), Name = "Admin" },
            new() { Id = Guid.NewGuid(), Name = "User" }
        };
        var cacheKey = "role:list";

        _mockKeyGenerator
            .Setup(x => x.GenerateListKey<Role>())
            .Returns(cacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<IReadOnlyList<Role>>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        var result = await _decorator.ListAsync();

        // Assert
        result.Should().BeEquivalentTo(roles);
        _mockRepository.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheService.Verify(x => x.GetAsync<IReadOnlyList<Role>>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAsync_WhenCacheMiss_ShouldFetchFromRepositoryAndCache()
    {
        // Arrange
        var roles = new List<Role>
        {
            new() { Id = Guid.NewGuid(), Name = "Admin" },
            new() { Id = Guid.NewGuid(), Name = "User" }
        };
        var cacheKey = "role:list";

        _mockKeyGenerator
            .Setup(x => x.GenerateListKey<Role>())
            .Returns(cacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<IReadOnlyList<Role>>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<Role>?)null);

        _mockRepository
            .Setup(x => x.ListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        var result = await _decorator.ListAsync();

        // Assert
        result.Should().BeEquivalentTo(roles);
        _mockRepository.Verify(x => x.ListAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<IReadOnlyList<Role>>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync<IReadOnlyList<Role>>(cacheKey, roles, It.IsAny<CacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddAsync_ShouldCallRepositoryAndInvalidateListCache()
    {
        // Arrange
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var entityCacheKey = $"role:{role.Id}";
        var listCacheKey = "role:list";

        _mockKeyGenerator
            .Setup(x => x.GenerateEntityKey<Role>(role.Id))
            .Returns(entityCacheKey);
        _mockKeyGenerator
            .Setup(x => x.GenerateListKey<Role>())
            .Returns(listCacheKey);

        _mockRepository
            .Setup(x => x.AddAsync(role, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        // Act
        var result = await _decorator.AddAsync(role);

        // Assert
        result.Should().Be(role);
        _mockRepository.Verify(x => x.AddAsync(role, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(entityCacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(listCacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldCallRepositoryAndInvalidateCaches()
    {
        // Arrange
        var role = new Role { Id = Guid.NewGuid(), Name = "Admin" };
        var entityCacheKey = $"role:{role.Id}";
        var listCacheKey = "role:list";

        _mockKeyGenerator
            .Setup(x => x.GenerateEntityKey<Role>(role.Id))
            .Returns(entityCacheKey);
        _mockKeyGenerator
            .Setup(x => x.GenerateListKey<Role>())
            .Returns(listCacheKey);

        _mockRepository
            .Setup(x => x.UpdateAsync(role, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _decorator.UpdateAsync(role);

        // Assert
        _mockRepository.Verify(x => x.UpdateAsync(role, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(entityCacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(listCacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_ShouldCallRepositoryAndInvalidateCaches()
    {
        // Arrange
        var id = Guid.NewGuid();
        var entityCacheKey = $"role:{id}";
        var listCacheKey = "role:list";

        _mockKeyGenerator
            .Setup(x => x.GenerateEntityKey<Role>(id))
            .Returns(entityCacheKey);
        _mockKeyGenerator
            .Setup(x => x.GenerateListKey<Role>())
            .Returns(listCacheKey);

        _mockRepository
            .Setup(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _decorator.DeleteAsync(id);

        // Assert
        _mockRepository.Verify(x => x.DeleteAsync(id, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(entityCacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.RemoveAsync(listCacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_WhenCacheHit_ShouldReturnCachedEntity()
    {
        // Arrange
        var name = "Admin";
        var role = new Role { Id = Guid.NewGuid(), Name = name };
        var cacheKey = $"role:name:{name}";

        _mockKeyGenerator
            .Setup(x => x.GenerateNameKey<Role>(name))
            .Returns(cacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        // Act
        var result = await _decorator.GetByNameAsync(name);

        // Assert
        result.Should().Be(role);
        _mockRepository.Verify(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()), Times.Never);
        _mockCacheService.Verify(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_WhenCacheMiss_ShouldFetchFromRepositoryAndCache()
    {
        // Arrange
        var name = "Admin";
        var role = new Role { Id = Guid.NewGuid(), Name = name };
        var cacheKey = $"role:name:{name}";

        _mockKeyGenerator
            .Setup(x => x.GenerateNameKey<Role>(name))
            .Returns(cacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        _mockRepository
            .Setup(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        // Act
        var result = await _decorator.GetByNameAsync(name);

        // Assert
        result.Should().Be(role);
        _mockRepository.Verify(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(cacheKey, role, It.IsAny<CacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByNameAsync_WhenRepositoryReturnsNull_ShouldNotCache()
    {
        // Arrange
        var name = "NonExistent";
        var cacheKey = $"role:name:{name}";

        _mockKeyGenerator
            .Setup(x => x.GenerateNameKey<Role>(name))
            .Returns(cacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        _mockRepository
            .Setup(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        // Act
        var result = await _decorator.GetByNameAsync(name);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<Role>(), It.IsAny<CacheEntryOptions>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CacheKeyGeneration_ShouldUseCorrectFormat()
    {
        // Arrange
        var id = Guid.NewGuid();
        var name = "Admin";
        var role = new Role { Id = id, Name = name };
        var entityCacheKey = $"role:{id}";
        var nameCacheKey = $"role:name:{name}";

        _mockKeyGenerator
            .Setup(x => x.GenerateEntityKey<Role>(id))
            .Returns(entityCacheKey);
        _mockKeyGenerator
            .Setup(x => x.GenerateNameKey<Role>(name))
            .Returns(nameCacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<Role>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        _mockRepository
            .Setup(x => x.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        _mockRepository
            .Setup(x => x.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        // Act
        await _decorator.GetAsync(id);
        await _decorator.GetByNameAsync(name);

        // Assert
        _mockCacheService.Verify(x => x.GetAsync<Role>(entityCacheKey, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(x => x.GetAsync<Role>(nameCacheKey, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CacheOptions_ShouldUseDefaultTTL()
    {
        // Arrange
        var id = Guid.NewGuid();
        var role = new Role { Id = id, Name = "Admin" };
        var cacheKey = $"role:{id}";

        _mockKeyGenerator
            .Setup(x => x.GenerateEntityKey<Role>(id))
            .Returns(cacheKey);

        _mockCacheService
            .Setup(x => x.GetAsync<Role>(cacheKey, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Role?)null);

        _mockRepository
            .Setup(x => x.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        CacheEntryOptions? capturedOptions = null;
        _mockCacheService
            .Setup(x => x.SetAsync(cacheKey, role, It.IsAny<CacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Callback<string, Role, CacheEntryOptions, CancellationToken>((key, value, options, ct) => capturedOptions = options);

        // Act
        await _decorator.GetAsync(id);

        // Assert
        capturedOptions.Should().NotBeNull();
        capturedOptions!.AbsoluteExpirationRelativeToNow.Should().Be(TimeSpan.FromMinutes(15)); // Default TTL for entities
    }
}
