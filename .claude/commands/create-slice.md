Create a vertical slice for a new use case following this project's architecture.

**Arguments:** $ARGUMENTS
**Expected format:** `{Entity} {UseCase} {command|query}`
**Examples:**
- `Books AddBook command`
- `Books GetBookById query`
- `Ratings RateBook command`

---

## Steps

### 1. Parse arguments

Extract:
- `Entity` — e.g. `Books` (PascalCase, plural)
- `UseCase` — e.g. `AddBook` (PascalCase verb+noun)
- `Type` — `command` or `query`

If arguments are missing or ambiguous, ask the user before proceeding.

---

### 2. Infer what files are needed

- **Command** slices: `{UseCase}Command.cs`, `{UseCase}Request.cs`, `{UseCase}Handler.cs`, `{UseCase}Validator.cs` (unless trivially no input). No response record — handler returns `Result`.
- **Query** slices: `{UseCase}Query.cs`, `{UseCase}Response.cs`, `{UseCase}Handler.cs`. Add `{UseCase}Request.cs` only if the query carries more than a single ID. No validator — queries don't mutate.

---

### 3. Create feature files

**`Features/{Entity}/{UseCase}/{UseCase}Command.cs`** (command type only)
```csharp
namespace BookRatings.Features.{Entity}.{UseCase};

public sealed record {UseCase}Command({UseCase}Request Request);
```

**`Features/{Entity}/{UseCase}/{UseCase}Query.cs`** (query type only)
```csharp
namespace BookRatings.Features.{Entity}.{UseCase};

public sealed record {UseCase}Query(/* parameters */);
```

**`Features/{Entity}/{UseCase}/{UseCase}Request.cs`** (command, or query with multiple params)
```csharp
namespace BookRatings.Features.{Entity}.{UseCase};

public sealed record {UseCase}Request(/* properties */);
```

**`Features/{Entity}/{UseCase}/{UseCase}Response.cs`** (query type only)
```csharp
namespace BookRatings.Features.{Entity}.{UseCase};

public sealed record {UseCase}Response(/* properties */);
```

**`Features/{Entity}/{UseCase}/{UseCase}Validator.cs`** (command type only)
```csharp
namespace BookRatings.Features.{Entity}.{UseCase};

public sealed class {UseCase}Validator
{
    public Result Validate({UseCase}Request request)
    {
        // add validation rules
        return Result.Ok();
    }
}
```

**`Features/{Entity}/{UseCase}/{UseCase}Handler.cs`**

For a command:
```csharp
using BookRatings.Features.{Entity};
using BookRatings.Shared.Abstractions;

namespace BookRatings.Features.{Entity}.{UseCase};

public sealed class {UseCase}Handler(I{Entity}Repository repository, {UseCase}Validator validator)
    : ICommandHandler<{UseCase}Command, Result>
{
    public async Task<Result> HandleAsync({UseCase}Command command, CancellationToken ct = default)
    {
        var validation = validator.Validate(command.Request);
        if (!validation.IsSuccess) return validation;

        // implement use case
        await repository.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

For a query:
```csharp
using BookRatings.Features.{Entity};
using BookRatings.Shared.Abstractions;

namespace BookRatings.Features.{Entity}.{UseCase};

public sealed class {UseCase}Handler(I{Entity}Repository repository)
    : IQueryHandler<{UseCase}Query, Result<{UseCase}Response>>
{
    public async Task<Result<{UseCase}Response>> HandleAsync({UseCase}Query query, CancellationToken ct = default)
    {
        // fetch and map to response
        return Result<{UseCase}Response>.Fail("Not implemented.");
    }
}
```

---

### 4. Check the entity repository interface

Open `Features/{Entity}/I{Entity}Repository.cs`. If the handler needs a method that is not yet declared, add it. If the file does not exist yet, create it:

```csharp
namespace BookRatings.Features.{Entity};

public interface I{Entity}Repository
{
    // add methods needed by this use case
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

Also check whether `Infrastructure/Persistence/{Entity}/Ef{Entity}Repository.cs` exists. If so, add the new method stub. If not, note it as a TODO for the user.

---

### 5. Register in DI

Open `Features/{Entity}/{Entity}ServiceExtensions.cs`. Add registrations for the new handler and validator (if applicable). If the file does not exist yet, create it:

```csharp
using BookRatings.Shared.Abstractions;

namespace BookRatings.Features.{Entity};

public static class {Entity}ServiceExtensions
{
    public static IServiceCollection Add{Entity}Features(this IServiceCollection services)
    {
        services.AddScoped<I{Entity}Repository, /* Ef{Entity}Repository */>();
        // handler registration
        return services;
    }
}
```

Remind the user to call `builder.Services.Add{Entity}Features()` in `Program.cs` if this is a new entity.

---

### 6. Create test stubs

**`tests/Features/{Entity}/{UseCase}/{UseCase}HandlerTests.cs`**
```csharp
namespace BookRatings.Tests.Features.{Entity}.{UseCase};

public sealed class {UseCase}HandlerTests
{
    private readonly I{Entity}Repository _repository = Substitute.For<I{Entity}Repository>();
    private readonly {UseCase}Handler _sut;

    public {UseCase}HandlerTests()
    {
        // construct _sut with _repository and any validators
    }

    [Fact]
    public async Task HandleAsync_ValidInput_Succeeds()
    {
        // arrange

        // act

        // assert
    }
}
```

**`tests/Features/{Entity}/{UseCase}/{UseCase}ValidatorTests.cs`** (command only)
```csharp
namespace BookRatings.Tests.Features.{Entity}.{UseCase};

public sealed class {UseCase}ValidatorTests
{
    private readonly {UseCase}Validator _sut = new();

    [Fact]
    public void Validate_ValidRequest_ReturnsOk()
    {
        // arrange + act + assert
    }
}
```

---

### 7. Final checklist

After creating all files, confirm:

- [ ] All new files are in `Features/{Entity}/{UseCase}/`
- [ ] Handler implements `ICommandHandler<,>` or `IQueryHandler<,>` from `Shared/Abstractions/`
- [ ] Entities are not returned — only response DTOs or `Result<T>`
- [ ] New repository methods are declared on the interface
- [ ] Handler and validator are registered in `{Entity}ServiceExtensions.cs`
- [ ] `Program.cs` calls `Add{Entity}Features()` (new entities only)
- [ ] Test stubs exist in `tests/Features/{Entity}/{UseCase}/`
