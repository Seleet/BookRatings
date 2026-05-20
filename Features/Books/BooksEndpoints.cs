using BookRatings.Features.Books.ListBooks;
using BookRatings.Shared.Abstractions;

namespace BookRatings.Features.Books;

public static class BooksEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/books");

        group.MapGet("/", async (
            IQueryHandler<ListBooksQuery, Result<IReadOnlyList<ListBooksResponse>>> handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(new ListBooksQuery(), ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(result.Error);
        });

        return app;
    }
}
