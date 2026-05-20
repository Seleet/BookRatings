namespace BookRatings.Features.Books;

public sealed class Book
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public required string Author { get; init; }
    public int PublishedYear { get; init; }
    public List<Rating> Ratings { get; init; } = [];

    public double AverageRating => Ratings.Count == 0 ? 0 : Ratings.Average(r => r.Score);
}
