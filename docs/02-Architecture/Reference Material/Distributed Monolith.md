# Distributed Monolith (Anti-Pattern)

## Table of Contents
- [Definition](#definition)
- [Characteristics](#characteristics)
- [Why It Happens](#why-it-happens)
- [Risks](#risks)
- [How to Avoid](#how-to-avoid)
- [When It’s Acceptable](#when-its-acceptable)

## Definition
- A system that *looks like* microservices (multiple deployables) but *acts like* a monolith.  
- Services are tightly coupled (shared DB, synchronous dependencies).  
- Scaling, deploying, or evolving services independently is nearly impossible.  

[^](#distributed-monolith-anti-pattern)

## Characteristics
- **Shared Database** → Multiple services directly tied to one DB schema.  
- **Tight Coupling** → Changes in one service force changes in others.  
- **Chatty Communication** → Excessive synchronous calls between services.  
- **Coordinated Deployments** → Must release everything together.  
- **Low Autonomy** → Teams can’t evolve services independently.  

[^](#distributed-monolith-anti-pattern)

## Why It Happens
- Splitting codebases without rethinking **domain boundaries** (ignoring DDD).  
- Overusing synchronous HTTP/REST calls instead of async messaging/events.  
- Keeping legacy monolithic database designs.  
- Treating microservices as just “smaller controllers” instead of business-aligned services.  

[^](#distributed-monolith-anti-pattern)

## Risks
- Complexity of distributed systems **without** the benefits.  
- **Deployment pain** → Breaks easily when one service is down.  
- **Scalability limits** → Can’t scale independently by workload.  
- **Team bottlenecks** → No true autonomy; slow feature delivery.  

[^](#distributed-monolith-anti-pattern)

## How to Avoid
- **Bounded Contexts (DDD)** → Design services around business domains.  
- **Independent Data Ownership** → Each service owns its own persistence.  
- **Asynchronous Messaging** → Use events, queues, streams (RabbitMQ, Kafka, Azure Service Bus).  
- **Resilience Patterns** → Circuit breakers, retries, eventual consistency.  
- **Autonomous Teams** → Each service deployable independently.  

[^](#distributed-monolith-anti-pattern)

## When It’s Acceptable
- For **small apps** where microservices are overkill, a modular **monolith** is often better.  
- Move to true microservices only when scaling, team autonomy, or complexity demands it.  

[^](#distributed-monolith-anti-pattern)
