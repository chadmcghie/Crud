# Microservices: What You Need to Know

## Table of Contents
- [Core Principles](#core-principles)
- [Benefits](#benefits)
- [Risks / Challenges](#risks--challenges)
- [Design Patterns](#design-patterns)
- [Infrastructure](#infrastructure)
- [Observability & Ops](#observability--ops)
- [When to Use](#when-to-use)
- [When Not to Use](#when-not-to-use)

## Core Principles
- **Independent Deployability** → Each service can be built, deployed, and scaled separately.  
- **Bounded Contexts (DDD)** → Services align with business domains, not technical layers.  
- **Decentralized Data** → Each service owns its database; no shared schema.  
- **Smart Endpoints, Dumb Pipes** → Services hold logic; communication is lightweight.  

[^](#microservices-what-you-need-to-know)

## Benefits
- **Scalability** → Scale hot services independently.  
- **Agility** → Small teams can own services end-to-end.  
- **Resilience** → Failures can be isolated.  
- **Tech Flexibility** → Different services can use different stacks.  

[^](#microservices-what-you-need-to-know)

## Risks / Challenges
- **Distributed Monolith** → Tightly coupled services that can’t deploy independently.  
- **Operational Complexity** → CI/CD, monitoring, versioning across dozens of services.  
- **Data Consistency** → Eventual consistency instead of global transactions.  
- **Network Reliance** → Latency, retries, circuit breakers are critical.  
- **Team Overhead** → Requires strong DevOps + observability discipline.  

[^](#microservices-what-you-need-to-know)

## Design Patterns
- **API Gateway** → Central entry point for routing, auth, rate limiting.  
- **Service Registry / Discovery** → Track service endpoints dynamically.  
- **Event-Driven Messaging** → RabbitMQ, Kafka, Azure Service Bus for async comms.  
- **CQRS & Event Sourcing** → Split read/write models, capture history as events.  
- **Saga Pattern** → Coordinate long-running distributed transactions.  
- **Strangler Fig** → Incrementally decompose a monolith into microservices.  

[^](#microservices-what-you-need-to-know)

## Infrastructure
- **Containers** → Docker for packaging.  
- **Orchestration** → Kubernetes, AKS, EKS, GKE for scaling & resilience.  
- **Service Mesh** → Istio, Linkerd for traffic, security, observability.  
- **Cloud-Native Services** → AWS Lambda, Azure Functions, Fargate, etc.  

[^](#microservices-what-you-need-to-know)

## Observability & Ops
- **Logging** → Centralized structured logs (ELK, Serilog, Seq).  
- **Metrics** → Prometheus, CloudWatch, App Insights.  
- **Tracing** → Distributed tracing (OpenTelemetry, Jaeger).  
- **Resilience** → Circuit breakers, retries, bulkheads (Polly in .NET).  

[^](#microservices-what-you-need-to-know)

## When to Use
- Large, complex domains needing independent scaling.  
- Teams aligned to business domains (2-pizza team rule).  
- High demand for frequent, independent deployments.  

[^](#microservices-what-you-need-to-know)

## When Not to Use
- Small apps (monolith or modular monolith is simpler).  
- Teams without mature CI/CD, monitoring, and automation.  
- Low scale/complexity problems where microservices add cost without benefit.  

[^](#microservices-what-you-need-to-know)
