# Feature Implementation Guide

This project uses **Vertical Slice Architecture**. Each feature lives in a self-contained folder that owns everything it needs — from the command/query through the handler, DTOs, and response objects. Cross-cutting concerns (persistence, auth, etc.) are shared infrastructure, not shared feature logic.

No third-party mediator library is used (MediatR is now a paid library). Dispatching is done via direct handler injection — handlers implement typed `ICommandHandler<,>` / `IQueryHandler<,>` interfaces defined under `Shared/Abstractions/` and are resolved from DI by callers. If you want to adopt MediatR, Brighter, or MassTransit instead, update this guide and the DI registrations accordingly.

---

## Folder Structure

```
Features/
└── {Entity}/                            e.g. Books/
    ├── I{Entity}Repository.cs           entity-scoped persistence interface
    ├── Add{Entity}()ServiceExtensions   per-feature DI registration
    └── {UseCase}/                       e.g. AddBook/, GetBookById/, RateBook/
        ├── {UseCase}Command.cs          or {UseCase}Query.cs
        ├── {UseCase}Handler.cs
        ├── {UseCase}Request.cs          input DTO (omit if no input)
        ├── {UseCase}Response.cs         output DTO (omit for void commands)
        └── {UseCase}Validator.cs        input validation (omit if trivial)

Infrastructure/
└── Persistence/
    ├── AppDbContext.cs
    └── {Entity}/
        └── Ef{Entity}Repository.cs      implements IBookRepository, etc.

Shared/
└── Abstractions/
    ├── ICommandHandler.cs
    ├── IQueryHandler.cs
    └── Result.cs

Components/
└── Pages/
    └── {Entity}/
        └── {UseCase}.razor              Blazor page (thin adapter only)

tests/                                   sibling project — BookRatings.Tests.csproj
└── Features/
    └── {Entity}/
        └── {UseCase}/
            ├── {UseCase}HandlerTests.cs
            └── {UseCase}ValidatorTests.cs
```

One class/interface/record per file. No partial classes.

### Naming conventions

| Artifact | Pattern | Example |
|---|---|---|
| Command | `{UseCase}Command` | `AddBookCommand` |
| Query | `{UseCase}Query` | `GetBookByIdQuery` |
| Handler | `{UseCase}Handler` | `AddBookHandler` |
| Input DTO | `{UseCase}Request` | `AddBookRequest` |
| Output DTO | `{UseCase}Response` | `GetBookByIdResponse` |
| Validator | `{UseCase}Validator` | `AddBookValidator` |
| Entity interface | `I{Entity}Repository` | `IBookRepository` |
| DI extension | `{Entity}ServiceExtensions` | `BookServiceExtensions` |

---

## Handler Interfaces

Every handler implements one of these two interfaces, defined in `Shared/Abstractions/`:

```csharp
// Shared/Abstractions/ICommandHandler.cs
namespace BookRatings.Shared.Abstractions;

public interface ICommandHandler<TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken ct = default);
}
```

```csharp
// Shared/Abstractions/IQueryHandler.cs
namespace BookRatings.Shared.Abstractions;

public interface IQueryHandler<TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken ct = default);
}
```

Use `Result<T>` as `TResult` for commands that can fail in expected ways (see **Result Types** below). Use the concrete response record for queries.

---

## Result Types

Use `Result<T>` (and `Result` for void commands) for **expected failures** — validation errors, not-found, business rule violations. Throw exceptions only for unexpected infrastructure failures.

```csharp
// Shared/Abstractions/Result.cs
namespace BookRatings.Shared.Abstractions;

public sealed class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }

    private Result(bool success, string? error) { IsSuccess = success; Error = error; }

    public static Result Ok() => new(true, null);
    public static Result Fail(string error) => new(false, error);
}

public sealed class Result<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public string? Error { get; }

    private Result(bool success, T? value, string? error) { IsSuccess = success; Value = value; Error = error; }

    public static Result<T> Ok(T value) => new(true, value, null);
    public static Result<T> Fail(string error) => new(false, default, error);
}
```

Handlers return `Result` / `Result<T>` — callers check `IsSuccess`, never catch business-rule exceptions.

---

## Anatomy of a Feature

### 1. Request DTO

```csharp
// Features/Books/AddBook/AddBookRequest.cs
namespace BookRatings.Features.Books.AddBook;

public sealed record AddBookRequest(string Title, string Author, int PublishedYear);
```

### 2. Command or Query record

```csharp
// Features/Books/AddBook/AddBookCommand.cs
namespace BookRatings.Features.Books.AddBook;

public sealed record AddBookCommand(AddBookRequest Request);
```

```csharp
// Features/Books/GetBookById/GetBookByIdQuery.cs
namespace BookRatings.Features.Books.GetBookById;

public sealed record GetBookByIdQuery(Guid BookId);
```

### 3. Response DTO

Never expose domain/entity objects beyond the handler boundary. Map at the handler.

```csharp
// Features/Books/GetBookById/GetBookByIdResponse.cs
namespace BookRatings.Features.Books.GetBookById;

public sealed record GetBookByIdResponse(Guid Id, string Title, string Author, double AverageRating);
```

### 4. Validator (when input is non-trivial)

```csharp
// Features/Books/AddBook/AddBookValidator.cs
namespace BookRatings.Features.Books.AddBook;

public sealed class AddBookValidator
{
    public Result Validate(AddBookRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return Result.Fail("Title is required.");
        if (string.IsNullOrWhiteSpace(request.Author))
            return Result.Fail("Author is required.");
        if (request.PublishedYear < 1 || request.PublishedYear > DateTime.UtcNow.Year)
            return Result.Fail("Published year is out of range.");
        return Result.Ok();
    }
}
```

### 5. Entity-scoped repository interface

```csharp
// Features/Books/IBookRepository.cs
namespace BookRatings.Features.Books;

public interface IBookRepository
{
    Task<Book?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Book book, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

### 6. Handler

Implements the typed interface. Depends on interfaces only. Maps entities to response DTOs before returning.

```csharp
// Features/Books/AddBook/AddBookHandler.cs
namespace BookRatings.Features.Books.AddBook;

public sealed class AddBookHandler(IBookRepository books, AddBookValidator validator)
    : ICommandHandler<AddBookCommand, Result>
{
    public async Task<Result> HandleAsync(AddBookCommand command, CancellationToken ct = default)
    {
        var validation = validator.Validate(command.Request);
        if (!validation.IsSuccess) return validation;

        var book = new Book(Guid.NewGuid(), command.Request.Title, command.Request.Author, command.Request.PublishedYear);
        await books.AddAsync(book, ct);
        await books.SaveChangesAsync(ct);
        return Result.Ok();
    }
}
```

```csharp
// Features/Books/GetBookById/GetBookByIdHandler.cs
namespace BookRatings.Features.Books.GetBookById;

public sealed class GetBookByIdHandler(IBookRepository books)
    : IQueryHandler<GetBookByIdQuery, Result<GetBookByIdResponse>>
{
    public async Task<Result<GetBookByIdResponse>> HandleAsync(GetBookByIdQuery query, CancellationToken ct = default)
    {
        var book = await books.FindByIdAsync(query.BookId, ct);
        if (book is null) return Result<GetBookByIdResponse>.Fail("Book not found.");

        return Result<GetBookByIdResponse>.Ok(new(book.Id, book.Title, book.Author, book.AverageRating));
    }
}
```

---

## Dependency Injection

Group registrations in a per-feature extension method. Lifetime defaults: **Scoped** for handlers, repositories, and validators; **Singleton** for stateless shared services.

```csharp
// Features/Books/BookServiceExtensions.cs
namespace BookRatings.Features.Books;

public static class BookServiceExtensions
{
    public static IServiceCollection AddBookFeatures(this IServiceCollection services)
    {
        // Infrastructure
        services.AddScoped<IBookRepository, EfBookRepository>();

        // Validators
        services.AddScoped<AddBookValidator>();

        // Handlers — register against their interface so callers can inject ICommandHandler<,>
        services.AddScoped<ICommandHandler<AddBookCommand, Result>, AddBookHandler>();
        services.AddScoped<IQueryHandler<GetBookByIdQuery, Result<GetBookByIdResponse>>, GetBookByIdHandler>();

        return services;
    }
}
```

```csharp
// Program.cs
builder.Services.AddBookFeatures();
// builder.Services.AddRatingFeatures();
// ...
```

---

## Blazor Pages

Inject the typed handler interface. Pages translate UI events to commands/queries and render results. No business logic in pages.

```razor
@* Components/Pages/Books/AddBook.razor *@
@page "/books/add"
@inject ICommandHandler<AddBookCommand, Result> Handler
@inject NavigationManager Nav

@if (error is not null)
{
    <p class="text-danger">@error</p>
}

<EditForm Model="request" OnValidSubmit="SubmitAsync">
    ...
</EditForm>

@code {
    private AddBookRequest request = new("", "", DateTime.UtcNow.Year);
    private string? error;

    private async Task SubmitAsync()
    {
        var result = await Handler.HandleAsync(new AddBookCommand(request));
        if (!result.IsSuccess) { error = result.Error; return; }
        Nav.NavigateTo("/books");
    }
}
```

## Minimal API Endpoints (when applicable)

Map endpoints close to the feature. One endpoint group per entity, one handler injection per endpoint.

```csharp
// Features/Books/BooksEndpoints.cs
namespace BookRatings.Features.Books;

public static class BooksEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/books");

        group.MapPost("/", async (
            AddBookRequest request,
            ICommandHandler<AddBookCommand, Result> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new AddBookCommand(request), ct);
            return result.IsSuccess ? Results.Created() : Results.BadRequest(result.Error);
        });

        group.MapGet("/{id:guid}", async (
            Guid id,
            IQueryHandler<GetBookByIdQuery, Result<GetBookByIdResponse>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new GetBookByIdQuery(id), ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound(result.Error);
        });

        return app;
    }
}
```

Register in `Program.cs`:
```csharp
app.MapBookEndpoints();
```

---

## Tests

Tests live in a sibling `tests/` project (`BookRatings.Tests.csproj`) that mirrors the `Features/` folder tree. **Test handlers and validators in isolation** — mock repository interfaces, never the database.

```
tests/
└── Features/
    └── Books/
        ├── AddBook/
        │   ├── AddBookHandlerTests.cs
        │   └── AddBookValidatorTests.cs
        └── GetBookById/
            └── GetBookByIdHandlerTests.cs
```

### Handler test pattern

```csharp
// tests/Features/Books/AddBook/AddBookHandlerTests.cs
namespace BookRatings.Tests.Features.Books.AddBook;

public sealed class AddBookHandlerTests
{
    private readonly IBookRepository _books = Substitute.For<IBookRepository>();
    private readonly AddBookHandler _sut;

    public AddBookHandlerTests()
    {
        _sut = new AddBookHandler(_books, new AddBookValidator());
    }

    [Fact]
    public async Task HandleAsync_ValidRequest_AddsBook()
    {
        var command = new AddBookCommand(new AddBookRequest("Clean Code", "Robert Martin", 2008));

        var result = await _sut.HandleAsync(command);

        Assert.True(result.IsSuccess);
        await _books.Received(1).AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_EmptyTitle_ReturnsFail()
    {
        var command = new AddBookCommand(new AddBookRequest("", "Author", 2020));

        var result = await _sut.HandleAsync(command);

        Assert.False(result.IsSuccess);
        await _books.DidNotReceive().AddAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>());
    }
}
```

The example uses [NSubstitute](https://nsubstitute.github.io/) for mocking and [xUnit](https://xunit.net/) as the test runner — adjust to your preferred libraries.

---

## New Feature Checklist

Use this when implementing any new use case:

- [ ] Create `Features/{Entity}/{UseCase}/` folder
- [ ] Add `{UseCase}Request.cs` record (input DTO)
- [ ] Add `{UseCase}Command.cs` or `{UseCase}Query.cs` record
- [ ] Add `{UseCase}Response.cs` record (output DTO, if applicable)
- [ ] Add `{UseCase}Validator.cs` (if input validation is needed)
- [ ] Add `{UseCase}Handler.cs` implementing `ICommandHandler<,>` or `IQueryHandler<,>` from `Shared/Abstractions/`
- [ ] Add or update `I{Entity}Repository.cs` if the handler needs new persistence methods
- [ ] Add repository implementation in `Infrastructure/Persistence/{Entity}/`
- [ ] Register handler, validator, and any new interfaces in `{Entity}ServiceExtensions.cs`
- [ ] Add Blazor page in `Components/Pages/{Entity}/` OR minimal API endpoint in `{Entity}Endpoints.cs`
- [ ] Entities never leave the handler — all returns are response DTOs or `Result<T>`
- [ ] Add corresponding handler/validator tests in `tests/Features/{Entity}/{UseCase}/`

---

## Rules of Thumb

- **One use case = one folder.** Never share a folder because two use cases share a DTO.
- **No cross-feature imports.** If two features share a type, promote it to `Shared/Abstractions/` (for interfaces/primitives) or `Shared/` (for anything else).
- **Interfaces at every layer boundary** — persistence, external HTTP, file system, clock.
- **Records for all DTOs.** Immutability and structural equality at no cost.
- **`sealed` on everything** not designed for inheritance.
- **Result types for expected failures; exceptions for infrastructure surprises.**
- **Handlers own business logic.** Pages, endpoints, and tests are thin adapters.
