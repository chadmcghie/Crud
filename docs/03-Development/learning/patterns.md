# Agent OS Learning Patterns

> Last Updated: 2025-09-08
> Auto-generated from resolved issues and development experience

## Testing Patterns

### SQLite Database Locking
**ID**: `sqlite-lock-error`  
**Problem**: SQLITE_BUSY errors when running tests in parallel  
**Symptoms**:
- `Error: SQLITE_BUSY: database is locked`
- `database table is locked`
- Tests fail intermittently

**Solution**:
```javascript
// playwright.config.ts
export default defineConfig({
  workers: 1, // Force serial execution
  use: {
    // Use unique database per test run
    baseURL: process.env.BASE_URL || 'http://localhost:5172',
  }
});
```

**Tags**: `sqlite`, `testing`, `e2e`, `database`, `lock`  
**References**: Issue #79, ADR-001

---

### Test Isolation Failures
**ID**: `test-isolation`  
**Problem**: Tests affecting each other's state  
**Symptoms**:
- Tests pass individually but fail when run together
- Inconsistent test results
- State leaking between tests

**Solution**:
1. Use unique database names per test
2. Implement proper cleanup in afterEach hooks
3. Reset application state between tests
4. Use WebApplicationFactory for integration tests

**Tags**: `testing`, `isolation`, `cleanup`, `state`

---

### Angular Test Timeout
**ID**: `angular-test-timeout`  
**Problem**: Angular tests timing out in CI  
**Symptoms**:
- `Jasmine timeout: async callback not invoked`
- Tests hang indefinitely
- CI pipeline failures

**Solution**:
```typescript
// Increase timeout in karma.conf.js
browserNoActivityTimeout: 60000,
browserDisconnectTimeout: 10000,
browserDisconnectTolerance: 3,
```

**Tags**: `angular`, `testing`, `timeout`, `ci`

## Build Patterns

### Slow Angular Builds
**ID**: `angular-build-slow`  
**Problem**: Angular builds taking excessive time (>5 minutes)  
**Symptoms**:
- Long CI/CD pipeline times
- Developer productivity impact
- Memory usage spikes

**Solution**:
```bash
# Skip Angular build when not needed
export SKIP_ANGULAR_BUILD=true

# Use incremental builds
ng build --configuration development --watch

# Optimize build configuration
"buildOptimizer": false,
"aot": false,
"sourceMap": false
```

**Tags**: `angular`, `build`, `performance`, `optimization`

---

### EF Core Migration Conflicts
**ID**: `ef-migration-conflict`  
**Problem**: Migration conflicts when multiple developers work on database  
**Symptoms**:
- `Migration has already been applied`
- Conflicting migration files
- Database schema out of sync

**Solution**:
1. Use feature branch migrations
2. Squash migrations before merging
3. Regenerate migrations on main branch
```bash
dotnet ef migrations remove
dotnet ef migrations add Combined_Migration
```

**Tags**: `ef-core`, `migrations`, `database`, `conflict`

---

### NuGet Package Restore Failures
**ID**: `nuget-restore-fail`  
**Problem**: Package restore fails with network or cache issues  
**Symptoms**:
- `Unable to load the service index`
- `Package not found`
- Intermittent restore failures

**Solution**:
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Use specific package source
dotnet restore --source https://api.nuget.org/v3/index.json

# Disable parallel restore
dotnet restore --disable-parallel
```

**Tags**: `nuget`, `packages`, `restore`, `cache`

## Error Patterns

### CORS Policy Violations
**ID**: `cors-error`  
**Problem**: Cross-Origin Resource Sharing blocks API calls  
**Symptoms**:
- `Access-Control-Allow-Origin` header missing
- Preflight request failures
- 403 Forbidden on OPTIONS requests

**Solution**:
```csharp
// Program.cs
builder.Services.AddCors(options =>
{
    options.AddPolicy("Development",
        builder => builder
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials());
});

app.UseCors("Development");
```

**Tags**: `cors`, `api`, `security`, `angular`

---

### Dependency Injection Errors
**ID**: `di-service-not-found`  
**Problem**: Service not registered in DI container  
**Symptoms**:
- `Unable to resolve service for type`
- `No service for type has been registered`
- Application startup failures

**Solution**:
1. Register service in Program.cs:
```csharp
builder.Services.AddScoped<IMyService, MyService>();
```
2. Check service lifetime (Scoped/Singleton/Transient)
3. Verify interface and implementation match
4. Check for circular dependencies

**Tags**: `di`, `dependency-injection`, `services`, `startup`

---

### Angular Routing 404
**ID**: `angular-route-404`  
**Problem**: Routes not found after deployment  
**Symptoms**:
- 404 errors on page refresh
- Direct URL access fails
- Works in development but not production

**Solution**:
1. Configure server for SPA routing:
```csharp
// Program.cs
app.MapFallbackToFile("index.html");
```
2. Set base href correctly:
```html
<base href="/">
```
3. Use HashLocationStrategy if needed

**Tags**: `angular`, `routing`, `deployment`, `spa`

## Performance Patterns

### N+1 Query Problem
**ID**: `ef-n-plus-one`  
**Problem**: Multiple database queries for related data  
**Symptoms**:
- Slow API responses
- High database load
- Multiple SQL queries in logs

**Solution**:
```csharp
// Use Include for eager loading
var items = await _context.Orders
    .Include(o => o.OrderItems)
    .ThenInclude(oi => oi.Product)
    .ToListAsync();

// Use projection for specific fields
var items = await _context.Orders
    .Select(o => new OrderDto
    {
        Id = o.Id,
        Items = o.OrderItems.Select(oi => oi.Name)
    })
    .ToListAsync();
```

**Tags**: `ef-core`, `performance`, `database`, `n+1`

---

### Memory Leaks in Angular
**ID**: `angular-memory-leak`  
**Problem**: Memory usage increases over time  
**Symptoms**:
- Browser becomes slow
- Page crashes after extended use
- Memory profiler shows retained objects

**Solution**:
```typescript
// Unsubscribe from observables
private destroy$ = new Subject<void>();

ngOnInit() {
  this.service.getData()
    .pipe(takeUntil(this.destroy$))
    .subscribe(data => this.data = data);
}

ngOnDestroy() {
  this.destroy$.next();
  this.destroy$.complete();
}
```

**Tags**: `angular`, `memory`, `rxjs`, `performance`

## Security Patterns

### JWT Token Expiration
**ID**: `jwt-token-expired`  
**Problem**: Authentication fails after token expires  
**Symptoms**:
- 401 Unauthorized errors
- User logged out unexpectedly
- API calls fail after period of time

**Solution**:
1. Implement token refresh mechanism
2. Add interceptor for automatic refresh:
```typescript
// auth.interceptor.ts
if (error.status === 401) {
  return this.authService.refreshToken()
    .pipe(
      switchMap(() => this.retryRequest(request))
    );
}
```
3. Configure appropriate token lifetimes

**Tags**: `jwt`, `authentication`, `security`, `token`

---

## Common Command Solutions

### Git Merge Conflicts
**ID**: `git-merge-conflict`  
**Problem**: Conflicts when merging branches  
**Solution**:
```bash
# Abort current merge
git merge --abort

# Pull latest changes
git pull origin main

# Rebase your branch
git rebase main

# Force push if needed (careful!)
git push --force-with-lease
```

**Tags**: `git`, `merge`, `conflict`, `version-control`

---

*This document is continuously updated as new patterns are discovered and resolved.*