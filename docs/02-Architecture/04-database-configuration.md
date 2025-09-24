# Database Configuration Guide

This document explains how to configure different database providers in the CRUD application.

## Available Database Providers

The application supports multiple database configurations:

### 1. SQLite (Current Production & Testing)
- **Use Case**: Production and end-to-end testing
- **Configuration**: `"DatabaseProvider": "SQLite"`
- **Description**: File-based database, lightweight and portable
- **Persistence**: Data persists in `.db` file
- **Testing Strategy**: Serial execution only (see ADR-001)
- **Limitations**: Single writer, no parallel test execution

### 2. Entity Framework In-Memory
- **Use Case**: Unit and integration testing
- **Configuration**: `"DatabaseProvider": "EntityFrameworkInMemory"`
- **Description**: In-memory database provider for EF Core
- **Persistence**: Data lost when process ends
- **Testing**: Good for isolated unit tests

### 3. SQL Server
- **Use Case**: Alternative production database (not currently used)
- **Configuration**: `"DatabaseProvider": "SqlServer"`
- **Description**: Full SQL Server database
- **Persistence**: Data persists between application restarts
- **Requirements**: Valid connection string in `ConnectionStrings:DefaultConnection`

## Configuration Examples

### Development (appsettings.Development.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CrudAppDev;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "DatabaseProvider": "EntityFrameworkInMemory"
}
```

### Production (appsettings.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CrudApp;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "DatabaseProvider": "SqlServer"
}
```

### End-to-End Testing (appsettings.Testing.json)
```json
{
  "DatabasePath": "${TEMP}/CrudTest_Serial.db",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=${TEMP}/CrudTest_Serial.db"
  },
  "DatabaseProvider": "SQLite"
}
```

## Testing Database Strategy

### Serial E2E Testing Requirements
Based on ADR-001 (Serial E2E Testing Decision), our testing strategy requires:

1. **Single Worker Execution**: Tests run sequentially with `workers: 1`
2. **Database Isolation**: Fresh SQLite file for each test
3. **File-Based Cleanup**: Delete and recreate `.db` file between tests
4. **Shared Servers**: One API and Angular server for all tests

### Why SQLite for Testing?
- Lightweight and fast for single-worker execution
- Easy cleanup (just delete the file)
- No server setup required
- Consistent behavior across environments

### Testing Database Locations
```
${TEMP}/CrudTest_Serial_[timestamp].db

Where ${TEMP} resolves to:
- Windows: %TEMP% or %TMP% (typically C:\Users\{user}\AppData\Local\Temp)
- Linux/Mac: $TMPDIR or /tmp
```

## Environment-Specific Configuration

You can override the database provider using environment variables:

```bash
# Use SQLite (Production & Testing)
export DatabaseProvider="SQLite"
export DatabasePath="${TEMP}/CrudApp.db"
export ConnectionStrings__DefaultConnection="Data Source=${TEMP}/CrudApp.db"

# Use Entity Framework In-Memory (Unit Tests)
export DatabaseProvider="EntityFrameworkInMemory"

# Use SQL Server (Alternative)
export DatabaseProvider="SqlServer"
export ConnectionStrings__DefaultConnection="Server=localhost;Database=CrudApp;Integrated Security=true;"
```

## Running with Different Configurations

### API Project
```bash
# Development (EF In-Memory)
dotnet run --project src/Api --environment Development

# Production (SQL Server)
dotnet run --project src/Api --environment Production

# End-to-End Testing (SQL Server)
dotnet run --project src/Api --environment E2E
```

### Minimal API Project
```bash
# Development (EF In-Memory)
dotnet run --project src/Api.Min --environment Development

# Production (SQL Server)
dotnet run --project src/Api.Min --environment Production
```

## Database Initialization

For Entity Framework providers (EntityFrameworkInMemory and SqlServer), the database is automatically created on application startup using `EnsureDatabaseAsync()`.

For SQL Server, this will:
1. Create the database if it doesn't exist
2. Apply the entity configurations
3. Create all tables and relationships

## Testing Scenarios

### Unit Tests
- Use `InMemory` provider for fast, isolated tests
- No database setup required

### Integration Tests
- Use `EntityFrameworkInMemory` to test EF behavior
- Tests repository implementations and entity configurations

### End-to-End Tests
- Use `SqlServer` provider for realistic testing
- Tests complete data persistence and retrieval workflows
- Requires database cleanup between test runs

## Connection String Formats

### SQLite (Current Default)
```
Data Source=${PROJECT_ROOT}/database.db    // Project-relative path
Data Source=${TEMP}/database.db            // Temp directory path  
Data Source=:memory:                       // For in-memory database
```

### SQL Server LocalDB
```
Server=(localdb)\\mssqllocaldb;Database=YourDatabase;Trusted_Connection=true;MultipleActiveResultSets=true
```

### SQL Server Express
```
Server=.\\SQLEXPRESS;Database=YourDatabase;Trusted_Connection=true;MultipleActiveResultSets=true
```

### SQL Server with Authentication
```
Server=your-server;Database=YourDatabase;User Id=your-user;Password=your-password;MultipleActiveResultSets=true
```

## Important: Parallel Testing Limitations

⚠️ **SQLite does not support parallel E2E testing!**

Due to SQLite's single-writer limitation and Entity Framework Core's constraints:
- E2E tests MUST run with `workers: 1` (serial execution)
- Each test gets a fresh database via file deletion
- Parallel execution causes database lock errors

See [ADR-001: Serial E2E Testing](./Decisions/0001-Serial-E2E-Testing.md) for full details.

## Troubleshooting

### Common Issues

1. **"Database is locked" errors during tests**
   - Ensure tests run serially (`workers: 1` in Playwright config)
   - Check that database file is properly deleted between tests
   - Verify no other processes are accessing the database

2. **"Connection string required" error**
   - Ensure `ConnectionStrings:DefaultConnection` is set
   - For SQLite, ensure `DatabasePath` is also configured

3. **Entity Framework errors**
   - Ensure all entity configurations are properly defined
   - Check for missing navigation properties or relationships
   - For SQLite, ensure migrations are compatible

### Debugging Tips

- Enable detailed logging for Entity Framework:
  ```json
  {
    "Logging": {
      "LogLevel": {
        "Microsoft.EntityFrameworkCore": "Information"
      }
    }
  }
  ```

- Use SQL Server Profiler or logs to monitor database queries
- Test connection strings using SQL Server Management Studio