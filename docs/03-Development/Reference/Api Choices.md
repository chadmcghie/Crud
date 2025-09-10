# Controllers vs Minimal APIs and Object Browser

## Table of Contents
- [Controllers (classic Web API)](#controllers-classic-web-api)
- [Minimal APIs](#minimal-apis)
- [What This Means](#what-this-means)

---

## Controllers (classic Web API)
- Controllers are **classes** (`UsersController : ControllerBase`).  
- They’re compiled into your assembly as **real types with methods**.  
- Visual Studio’s **Object Browser shows them**, because it lists types/namespaces.  
- Each action method is just a **`public` method** on a class.  

[^Back to top](#controllers-vs-minimal-apis-and-object-browser)

---

## Minimal APIs
- Endpoints (`app.MapGet`, `app.MapPost`, etc.) are just **extension methods** on `IEndpointRouteBuilder`.  
- When you call them in `Program.cs` (or in endpoint classes you organize yourself), they **don’t produce a new type** — they return a builder object.  
- The compiler generates **no `UserController` or `WeatherForecastController` class**.  
- That’s why **Object Browser shows nothing**: there’s no class, just runtime-registered routes.  

[^Back to top](#controllers-vs-minimal-apis-and-object-browser)

---

## What This Means
- **With Controllers** → You can “browse” API surface by looking at types.  
- **With Minimal APIs** → The routes exist only at runtime, so tooling like Object Browser can’t show them.  

Instead, you’ll need:
- **Swagger / OpenAPI** (`builder.Services.AddEndpointsApiExplorer(); builder.Services.AddSwaggerGen();`) to list endpoints.  
- Or **documentation tools** like Carter or Minimal API analyzers.  

### Bottom Line
- In the **Controllers version**, endpoints are visible because they’re classes.  
- In the **Minimal API version**, endpoints are *invisible* to Object Browser because they’re methods, not types.  

[^Back to top](#controllers-vs-minimal-apis-and-object-browser)
