namespace BookRatings.Features.Books.ListBooks;

public sealed record ListBooksResponse(
    Guid Id,
    string Title,
    string Author,
    int PublishedYear,
    double AverageRating,
    int RatingCount);
