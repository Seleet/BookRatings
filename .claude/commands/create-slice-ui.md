Create the Blazor UI page for an existing vertical slice use case.

**Arguments:** $ARGUMENTS
**Expected format:** `{Entity} {UseCase} {command|query}`
**Examples:**
- `Books AddBook command`
- `Books GetBookById query`
- `Books ListBooks query`

Run `/create-slice` first if the handler does not exist yet.

---

## Steps

### 1. Parse arguments

Extract:
- `Entity` — e.g. `Books`
- `UseCase` — e.g. `AddBook`
- `Type` — `command` or `query`

If arguments are missing, ask the user before proceeding.

---

### 2. Infer the page shape

- **Command** → a form page. Uses `EditForm` + `DataAnnotationsValidator`. Displays a success redirect or an inline error on failure.
- **Query (single item)** → a detail page. Fetches one record by ID from a route parameter. Shows a not-found message on failure.
- **Query (list/collection)** → a list page. Fetches a collection on `OnInitializedAsync`. Shows an empty state.

Ask the user if the intent is ambiguous (e.g. `GetBooks` could be either a list or a search).

---

### 3. Determine the route

Default convention: `/{ entity-kebab }/{ action-kebab }` for commands, `/{ entity-kebab }` or `/{ entity-kebab }/{ id }` for queries.

Examples:
- `AddBook` → `@page "/books/add"`
- `GetBookById` → `@page "/books/{Id:guid}"`
- `ListBooks` → `@page "/books"`
- `EditBook` → `@page "/books/{Id:guid}/edit"`

Confirm with the user if the route is non-obvious.

---

### 4. Create the Blazor page

File: **`Components/Pages/{Entity}/{UseCase}.razor`**

#### Command page template

```razor
@page "/{ route }"
@inject ICommandHandler<{UseCase}Command, Result> Handler
@inject NavigationManager Nav

<PageTitle>{ Human-readable title }</PageTitle>

<h1>{ Human-readable title }</h1>

@if (error is not null)
{
    <div class="alert alert-danger">@error</div>
}

<EditForm Model="request" OnValidSubmit="SubmitAsync">
    <DataAnnotationsValidator />

    @* Add InputText / InputNumber / etc. for each property on {UseCase}Request *@

    <button type="submit" class="btn btn-primary">Submit</button>
</EditForm>

@code {
    private {UseCase}Request request = new(/* defaults */);
    private string? error;

    private async Task SubmitAsync()
    {
        error = null;
        var result = await Handler.HandleAsync(new {UseCase}Command(request));
        if (!result.IsSuccess) { error = result.Error; return; }
        Nav.NavigateTo("/{ list-route }");
    }
}
```

#### Query — detail page template

```razor
@page "/{ route }/{Id:guid}"
@inject IQueryHandler<{UseCase}Query, Result<{UseCase}Response>> Handler

<PageTitle>{ Human-readable title }</PageTitle>

@if (response is null && error is null)
{
    <p>Loading...</p>
}
else if (error is not null)
{
    <div class="alert alert-danger">@error</div>
}
else
{
    <h1>@response!.{ PrimaryDisplayField }</h1>
    @* render remaining fields *@
}

@code {
    [Parameter] public Guid Id { get; set; }

    private {UseCase}Response? response;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        var result = await Handler.HandleAsync(new {UseCase}Query(Id));
        if (result.IsSuccess) response = result.Value;
        else error = result.Error;
    }
}
```

#### Query — list page template

```razor
@page "/{ route }"
@inject IQueryHandler<{UseCase}Query, Result<IReadOnlyList<{UseCase}Response>>> Handler

<PageTitle>{ Human-readable title }</PageTitle>

<h1>{ Human-readable title }</h1>

@if (items is null)
{
    <p>Loading...</p>
}
else if (!items.Any())
{
    <p>No { entity-display-name } found.</p>
}
else
{
    <ul>
        @foreach (var item in items)
        {
            <li>@item.{ PrimaryDisplayField }</li>
        }
    </ul>
}

@code {
    private IReadOnlyList<{UseCase}Response>? items;
    private string? error;

    protected override async Task OnInitializedAsync()
    {
        var result = await Handler.HandleAsync(new {UseCase}Query());
        if (result.IsSuccess) items = result.Value;
        else error = result.Error;
    }
}
```

Fill in actual property names from the real `{UseCase}Request` and `{UseCase}Response` records. Do not use placeholder field names in the final file.

---

### 5. Add nav link (optional)

If this page should appear in the sidebar, open `Components/Layout/NavMenu.razor` and add:

```razor
<div class="nav-item px-3">
    <NavLink class="nav-link" href="{ route }">
        <span class="bi bi-{ icon }" aria-hidden="true"></span> { Label }
    </NavLink>
</div>
```

Ask the user whether to add the nav link before making this change.

---

### 6. Final checklist

- [ ] Page is in `Components/Pages/{Entity}/`
- [ ] Route follows the naming convention (kebab-case entity + action)
- [ ] Handler is injected as the interface (`ICommandHandler<,>` / `IQueryHandler<,>`), not the concrete class
- [ ] All field names come from the real request/response records — no placeholders
- [ ] `error` state is displayed to the user
- [ ] `Loading...` state is shown before data arrives (query pages)
- [ ] No business logic in the page — only UI state and handler calls
