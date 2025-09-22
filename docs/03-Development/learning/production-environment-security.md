# Production Environment Variable Security

## Problem Statement
Our ConditionalAuthorizeAttribute bypasses authorization when `ASPNETCORE_ENVIRONMENT=Testing`. If this environment setting is compromised in production, it creates a critical security vulnerability.

## Multi-Layer Security Approach

### 1. **Infrastructure Security (Primary Defense)**

#### Docker/Container Security
```dockerfile
# Dockerfile - Hard-code production environment
FROM mcr.microsoft.com/dotnet/aspnet:8.0
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:80

# Don't allow runtime overrides
USER appuser
WORKDIR /app
COPY . .
ENTRYPOINT ["dotnet", "Api.dll"]
```

#### Kubernetes Security
```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: crud-api-prod
spec:
  template:
    spec:
      containers:
      - name: api
        image: crud-api:latest
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"  # Hard-coded, not from ConfigMap
        securityContext:
          runAsNonRoot: true
          readOnlyRootFilesystem: true
          allowPrivilegeEscalation: false
          capabilities:
            drop:
            - ALL
        resources:
          limits:
            memory: "512Mi"
            cpu: "500m"
      securityContext:
        runAsUser: 1001
        runAsGroup: 1001
        fsGroup: 1001
```

#### Cloud Platform Security

**Azure App Service:**
```bash
# Use Application Settings (more secure than environment variables)
az webapp config appsettings set \
  --resource-group prod-rg \
  --name crud-api-prod \
  --settings ASPNETCORE_ENVIRONMENT=Production

# Lock down with RBAC - only specific roles can modify
az role assignment create \
  --assignee prod-admin@company.com \
  --role "Website Contributor" \
  --scope /subscriptions/.../resourceGroups/prod-rg

# Enable managed identity
az webapp identity assign \
  --resource-group prod-rg \
  --name crud-api-prod
```

**AWS ECS/Fargate:**
```json
{
  "family": "crud-api-prod",
  "networkMode": "awsvpc",
  "requiresCompatibilities": ["FARGATE"],
  "taskRoleArn": "arn:aws:iam::account:role/crud-api-prod-task-role",
  "executionRoleArn": "arn:aws:iam::account:role/crud-api-prod-execution-role",
  "containerDefinitions": [
    {
      "name": "api",
      "environment": [
        {
          "name": "ASPNETCORE_ENVIRONMENT",
          "value": "Production"
        }
      ],
      "secrets": [
        {
          "name": "DATABASE_CONNECTION",
          "valueFrom": "arn:aws:secretsmanager:region:account:secret:prod/database"
        }
      ]
    }
  ]
}
```

### 2. **Application Security (Secondary Defense)**

#### ConditionalAuthorizeAttribute Safeguards
Our implementation includes multiple safeguards:

```csharp
public class ConditionalAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Safeguard 1: Fail-closed on context issues
        if (context?.HttpContext?.RequestServices == null)
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        try
        {
            var environment = context.HttpContext.RequestServices
                .GetRequiredService<IWebHostEnvironment>();

            if (environment.IsEnvironment("Testing"))
            {
                // Safeguard 2: Hostname validation
                var host = context.HttpContext.Request.Host.Host;
                if (host.Contains("prod") || host.Contains("api.") || host.EndsWith(".com"))
                {
                    context.Result = new ForbidResult("Testing environment detected on production-like host");
                    return;
                }

                return; // Allow testing bypass
            }

            // Normal authorization for non-Testing environments
            // ... authorization logic
        }
        catch (Exception)
        {
            // Safeguard 3: Fail-closed on exceptions
            context.Result = new UnauthorizedResult();
            return;
        }
    }
}
```

### 3. **Monitoring & Detection (Tertiary Defense)**

#### Environment Monitoring
```csharp
// In Program.cs - Log environment on startup
app.Logger.LogCritical("Application starting in {Environment} environment",
    app.Environment.EnvironmentName);

// Alert on unexpected environment
if (app.Environment.IsEnvironment("Testing") &&
    !app.Configuration.GetValue<bool>("AllowTestingInThisEnvironment"))
{
    throw new InvalidOperationException("Testing environment not allowed in this deployment");
}
```

#### Health Check with Environment Validation
```csharp
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        var environment = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
        var response = new
        {
            Status = report.Status.ToString(),
            Environment = environment.EnvironmentName,
            SecurityWarning = environment.IsEnvironment("Testing") ? "TESTING ENVIRONMENT ACTIVE" : null
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});
```

### 4. **CI/CD Pipeline Security**

#### Production Deployment Pipeline
```yaml
# GitHub Actions - Production deployment
name: Deploy to Production
on:
  push:
    branches: [main]

jobs:
  deploy:
    runs-on: ubuntu-latest
    environment: production  # Requires approval
    steps:
    - uses: actions/checkout@v4

    - name: Validate Environment Config
      run: |
        # Ensure no Testing environment in production configs
        if grep -r "ASPNETCORE_ENVIRONMENT.*Testing" deployment/; then
          echo "ERROR: Testing environment found in production config"
          exit 1
        fi

    - name: Deploy with locked environment
      run: |
        # Deploy with explicitly set production environment
        docker build --build-arg ASPNETCORE_ENVIRONMENT=Production .
        # Deploy to production with infrastructure-level environment lock
```

### 5. **Security Verification Checklist**

#### Pre-Deployment Verification
- [ ] Environment variable is set at infrastructure level (not application level)
- [ ] Container/VM has read-only filesystem where possible
- [ ] Application runs as non-root user
- [ ] RBAC/IAM policies restrict environment variable modification
- [ ] Monitoring alerts on unexpected environment settings
- [ ] Health checks include environment validation

#### Runtime Verification
- [ ] `/health` endpoint shows correct environment
- [ ] Logs confirm production environment on startup
- [ ] No "Testing environment" warnings in monitoring
- [ ] Authorization working correctly on protected endpoints

### 6. **Emergency Response Plan**

#### If Testing Environment Detected in Production:

1. **Immediate Actions:**
   ```bash
   # Stop the service immediately
   kubectl scale deployment crud-api-prod --replicas=0

   # Or for container services
   docker stop $(docker ps -q --filter ancestor=crud-api:latest)
   ```

2. **Investigation:**
   - Check deployment logs for how environment was set
   - Review recent configuration changes
   - Verify infrastructure access logs

3. **Remediation:**
   - Redeploy with correct environment configuration
   - Update infrastructure to prevent recurrence
   - Review and update access controls

### 7. **Testing the Security**

#### Simulate Attack Scenarios
```bash
# Test 1: Try to override environment at runtime (should fail)
docker run -e ASPNETCORE_ENVIRONMENT=Testing crud-api:prod

# Test 2: Try Testing environment on production-like host (should fail)
curl -H "Host: api.production.com" http://localhost:5172/api/people

# Test 3: Verify normal operation in actual Testing environment
ASPNETCORE_ENVIRONMENT=Testing dotnet run --no-launch-profile --urls http://localhost:5172
```

## Key Takeaways

1. **Defense in Depth**: Multiple layers prevent single point of failure
2. **Infrastructure First**: Primary security at deployment/infrastructure level
3. **Application Safeguards**: Secondary validation in code
4. **Monitoring**: Detect and alert on anomalies
5. **Fail-Closed**: When in doubt, deny access

## References

- [ASP.NET Core Security Best Practices](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Container Security Best Practices](https://kubernetes.io/docs/concepts/security/)
- [Azure App Service Security](https://docs.microsoft.com/en-us/azure/app-service/security-recommendations)
- [AWS Security Best Practices](https://aws.amazon.com/architecture/security-identity-compliance/)