using BookRatings.Features.Books.ListBooks;
using BookRatings.Infrastructure.Persistence.Books;
using BookRatings.Shared.Abstractions;

namespace BookRatings.Features.Books;

public static class BookServiceExtensions
{
    public static IServiceCollection AddBookFeatures(this IServiceCollection services)
    {
        services.AddScoped<IBookRepository, EfBookRepository>();

        services.AddScoped<
            IQueryHandler<ListBooksQuery, Result<IReadOnlyList<ListBooksResponse>>>,
            ListBooksHandler>();

        return services;
    }
}
