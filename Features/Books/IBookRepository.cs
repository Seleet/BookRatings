namespace BookRatings.Features.Books;

public interface IBookRepository
{
    Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct = default);
}
