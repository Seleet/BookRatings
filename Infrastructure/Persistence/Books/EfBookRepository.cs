using BookRatings.Features.Books;
using Microsoft.EntityFrameworkCore;

namespace BookRatings.Infrastructure.Persistence.Books;

public sealed class EfBookRepository(AppDbContext context) : IBookRepository
{
    public async Task<IReadOnlyList<Book>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Books
            .Include(b => b.Ratings)
            .AsNoTracking()
            .ToListAsync(ct);
    }
}
