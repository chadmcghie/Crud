# Software Design Guidelines (Cheat Sheet)

## Table of Contents
- [Guidelines](#guidelines)
- [Common Software Design Patterns](#common-software-design-patterns)
  - [Creational](#creational)
  - [Structural](#structural)
  - [Behavioral](#behavioral)
  - [Architectural / Enterprise Patterns](#architectural--enterprise-patterns)

## Guidelines
- **SOLID** → Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion.  
- **DRY** → Don’t Repeat Yourself; centralize shared logic.  
- **KISS** → Keep It Simple, Stupid; avoid unnecessary complexity.  
- **YAGNI** → You Aren’t Gonna Need It; don’t over-engineer.  
- **DDD** → Domain-Driven Design; model core business domain, use ubiquitous language.  
- **TDD** → Test-Driven Development; write tests before code.  
- **BDD** → Behavior-Driven Development; focus on business-readable specs.  
- **CQRS** → Command Query Responsibility Segregation; separate reads from writes.  
- **Event Sourcing** → Store state as sequence of events, not snapshots.  
- **Hexagonal / Clean Architecture** → Separate domain, application, and infrastructure layers.  
- **12-Factor App** → Best practices for cloud-native software (config, logs, disposability, etc.).  
- **Fail Fast** → Detect and surface errors early.  
- **Loose Coupling & High Cohesion** → Components interact minimally, but internally consistent.  
- **Separation of Concerns** → Each layer/component has a distinct purpose.  
- **Defense in Depth** → Multiple layers of validation, logging, security.  

[^](#software-design-guidelines-cheat-sheet)

# Common Software Design Patterns

## Creational
- **Singleton** → Ensure a class has only one instance.  
- **Factory Method** → Delegate object creation to subclasses.  
- **Abstract Factory** → Create families of related objects without specifying classes.  
- **Builder** → Construct complex objects step by step.  
- **Prototype** → Clone existing objects instead of building new from scratch.  

[^](#software-design-guidelines-cheat-sheet)

## Structural
- **Adapter** → Bridge incompatible interfaces.  
- **Decorator** → Add responsibilities to objects dynamically.  
- **Facade** → Provide a unified interface to a subsystem.  
- **Proxy** → Control access to another object.  
- **Composite** → Treat individual objects and groups uniformly (tree structures).  
- **Bridge** → Separate abstraction from implementation.  
- **Flyweight** → Reuse shared objects to save memory.  

[^](#software-design-guidelines-cheat-sheet)

## Behavioral
- **Observer** → One-to-many dependency (event subscription).  
- **Mediator** → Centralize complex communications.  
- **Strategy** → Swap algorithms at runtime.  
- **Command** → Encapsulate actions as objects.  
- **Chain of Responsibility** → Pass requests along a chain until handled.  
- **Template Method** → Define algorithm skeleton, let subclasses fill steps.  
- **State** → Change behavior when object’s state changes.  
- **Memento** → Capture/restore object state (undo).  
- **Visitor** → Add new operations to existing objects without modifying them.  
- **Interpreter** → Define a language grammar & interpreter.  
- **Iterator** → Sequentially access collection elements without exposing structure.  

[^](#software-design-guidelines-cheat-sheet)

## Architectural / Enterprise Patterns
- **Repository** → Abstraction for data access.  
- **Unit of Work** → Manage transactional consistency.  
- **CQRS** → Separate commands (writes) from queries (reads).  
- **Event Sourcing** → Store state as immutable events.  
- **Saga / Process Manager** → Coordinate long-running transactions.  
- **Domain Events** → Notify other parts of system about changes.  
- **Dependency Injection** → Externalize creation and binding of dependencies.  

[^](#software-design-guidelines-cheat-sheet)
