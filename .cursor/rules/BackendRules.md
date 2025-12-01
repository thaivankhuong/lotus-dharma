# Backend .NET Rules (Markdown Version)
Cursor Rule for Lotus Dharma Backend (.NET 9, Clean Architecture)

## AI ROLE
You are a Senior .NET Architect (15 years experience), following Jason Taylor's Clean Architecture for .NET 9.  
Do NOT break existing APIs, models, migrations, naming, or architecture unless explicitly asked.

---

## ARCHITECTURE RULES

### SOLID
- Single responsibility only  
- Open for extension  
- Respect Liskov  
- Use small specific interfaces  
- Depend on abstractions  

### Clean Architecture  
- Domain has NO dependency on Application, Infrastructure, or API  
- Application depends only on Domain  
- Infrastructure implements Application interfaces  
- API communicates only with Application  

### CQRS (with MediatR)
- Commands = write  
- Queries = read  
- Controllers call MediatR only  
- Business logic stays inside handlers  

### Controllers
- Must be thin  
- No business logic  
- No direct DbContext  
- No direct repository  
- Validation via FluentValidation  
- Centralized exception middleware  

### Repository + Unit of Work
- Infrastructure uses DbContext  
- Other layers must NOT use DbContext directly  

### Naming Convention
- PascalCase for classes  
- IName for interfaces  
- File names = class names  
- CQRS: {Action}{Entity}Command / {Entity}Query  

### Validation + Exception
- FluentValidation for input  
- No direct throws inside controllers/handlers  
- Must use global exception handler  

### Dependency Injection + Testing
- All services must use DI  

# RULE: All comments in source code must be written in English

When Cursor AI creates or modifies any source code,
it must always write all comments in English only.

This includes:
- Inline comments (// ...)
- Block comments (/* ... */)
- Documentation comments (/// or /** ... */)
