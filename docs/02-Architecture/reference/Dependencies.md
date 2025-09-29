# Software Architecture Decision Matrix (.NET 8+)

## Table of Contents
- [Decision Matrix](#decision-matrix)

## Decision Matrix
| Category          | Mainstream Choice                | Lightweight / Performance   | Enterprise / Advanced             |
|-------------------|----------------------------------|-----------------------------|-----------------------------------|
| **Framework**     | .NET 8+                          | Mono / Blazor               | Polyglot mix (Java, Node, etc.)   |
| **ORM / Data**    | Entity Framework Core            | Dapper / RepoDb / PetaPoco  | NHibernate / LLBLGen Pro          |
| **Object Mapping**| AutoMapper                       | Mapster / ValueInjecter     | AgileMapper / Manual mapping      |
| **Unit Testing**  | xUnit / NUnit / MSTest           | —                           | Integration: Testcontainers.NET   |
| **Mocks**         | Moq                              | NSubstitute / FakeItEasy    | —                                 |
| **E2E Testing**   | Playwright                       | Selenium / Cypress          | SpecFlow (BDD)                    |
| **Caching**       | MemoryCache (in-process)         | LazyCache                   | Redis / NCache (distributed)      |
| **Messaging**     | MediatR (in-process CQRS)        | CAP (lightweight event bus) | MassTransit / NServiceBus         |
| **Validation**    | FluentValidation                 | DataAnnotations             | Custom domain validators          |
| **Logging**       | Serilog                          | NLog                        | AppInsights / ELK Stack           |
| **Database**      | SQL Server / PostgreSQL          | SQLite / MySQL              | MongoDB / Cosmos DB / Neo4j       |
| **Identity**      | ASP.NET Core Identity            | External IdPs (Auth0, Okta) | Duende IdentityServer (OIDC/OAuth)|
| **Tokens**        | JWT (Microsoft.AspNetCore.\*)    | Paseto                      | Reference Tokens (IdP managed)    |
| **API Layer**     | ASP.NET Core Web API (REST)      | gRPC                        | GraphQL (Hot Chocolate) / OData   |
| **Front-End**     | Blazor                           | Angular / React / Vue       | —                                 |
| **DevOps / CI-CD**| GitHub Actions / Azure Pipelines | Docker + Terraform          | Kubernetes + Helm + Pulumi        |

[^](#software-architecture-decision-matrix-net-8)
