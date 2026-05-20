using BookRatings.Shared.Abstractions;

namespace BookRatings.Features.Books.ListBooks;

public sealed class ListBooksHandler(IBookRepository repository)
    : IQueryHandler<ListBooksQuery, Result<IReadOnlyList<ListBooksResponse>>>
{
    public async Task<Result<IReadOnlyList<ListBooksResponse>>> HandleAsync(
        ListBooksQuery query,
        CancellationToken ct = default)
    {
        var books = await repository.GetAllAsync(ct);

        IReadOnlyList<ListBooksResponse> response = books
            .Select(b => new ListBooksResponse(
                b.Id,
                b.Title,
                b.Author,
                b.PublishedYear,
                Math.Round(b.AverageRating, 2),
                b.Ratings.Count))
            .ToList();

        return Result<IReadOnlyList<ListBooksResponse>>.Ok(response);
    }
}
