# Database Configuration Guide

This document explains how to configure different database providers in the CRUD application.

## Available Database Providers

The application supports three database configurations:

### 3. SQLITE
- **Use Case**: Production and end-to-end testing
- **Configuration**: `"DatabaseProvider": "SqlServer"`
- **Description**: Uses Entity Framework with SQL Server database
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

### End-to-End Testing (appsettings.E2E.json)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=CrudAppE2E;Trusted_Connection=true;MultipleActiveResultSets=true"
  },
  "DatabaseProvider": "SqlServer"
}
```

## Environment-Specific Configuration

You can override the database provider using environment variables:

```bash
# Use SQL Server
export DatabaseProvider="SqlServer"
export ConnectionStrings__DefaultConnection="Server=localhost;Database=CrudApp;Integrated Security=true;"

# Use Entity Framework In-Memory
export DatabaseProvider="EntityFrameworkInMemory"

# Use Simple In-Memory
export DatabaseProvider="InMemory"
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

## Troubleshooting

### Common Issues

1. **"Connection string required" error**
   - Ensure `ConnectionStrings:DefaultConnection` is set when using SqlServer provider

2. **SQL Server connection failures**
   - Verify SQL Server/LocalDB is installed and running
   - Check connection string format and credentials

3. **Entity Framework errors**
   - Ensure all entity configurations are properly defined
   - Check for missing navigation properties or relationships

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