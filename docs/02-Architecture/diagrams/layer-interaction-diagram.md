Here's how the data flows between the layers:

NEED TO REVIEW THIS>>> THE DOMAIN LAYER DOES NOT DEPEND ON ANYTHING 
THE CHART MAKES IT LOOK LIKE THATS THE FLOW... ITS NOT


   🌍 Client (Web, Mobile, Postman)
                      │
                      ▼
             ┌──────────────────────┐
             │      API Layer       │
             │ (Controllers, DTOs)  │
             └──────────────────────┘
                  |    ▲
   API DTOs (data)|    |API DTOs (data)
                  ▼    |
             ┌───────────────────────┐
             │  Application Layer    │
             │ (Commands, Queries,   │
             │  Handlers, Interfaces)│
             └───────────────────────┘
                  |    ▲
 Interfaces (ops) |    | Entities/VOs
                  ▼    |
             ┌───────────────────────┐
             │    Domain Layer       │
             │ (Entities, VOs, Rules)│
             └───────────────────────┘
                  |    ▲
 Interfaces (ops) |    | Entities/VOs
                  ▼    |
             ┌───────────────────────┐
             │ Infrastructure Layer  │
             │ (EF, SQL, APIs,       │
             │  Repository Impl)     │
             └───────────────────────┘
