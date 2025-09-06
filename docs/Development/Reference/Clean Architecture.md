# CLEAN Architecture Principles (Nutshell)

## Table of Contents
- [Overview](#overview)
- [Key Ideas](#key-ideas)
- [Layering](#layering)

## Overview
- (**C**) Cross-cutting concerns separated → Logging, security, validation handled in their own layer.  
- (**L**) Layers of abstraction → Domain independent of frameworks; infrastructure on the outside.  
- (**E**) Entities at the core → Pure business rules, unaffected by tech or delivery mechanism.  
- (**A**) APIs as boundaries → Use interfaces/contracts between layers; dependencies flow inward.  
- (**N**) Neutral frameworks → Don’t tie domain logic to UI, DB, or external tech.  

[^](#clean-architecture-principles-nutshell)

## Key Ideas
- **Dependency Rule** → Source code dependencies point *inward* (outer layers depend on inner).  
- **Inner Circle = Business** → Entities, use cases.  
- **Outer Circle = Details** → DB, UI, external services, frameworks.  
- **Testability** → Business logic is framework-agnostic, easy to unit test.  
- **Flexibility** → Swap out details (DB, UI) without rewriting core.  

[^](#clean-architecture-principles-nutshell)

## Layering
- **Entities** → Business rules (core).  
- **Use Cases / Application Layer** → Orchestrates business processes.  
- **Interface Adapters** → Controllers, Presenters, Gateways.  
- **Frameworks & Drivers** → UI, DB, external services.  

[^](#clean-architecture-principles-nutshell)
