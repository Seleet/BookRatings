namespace BookRatings.Features.Books;

public sealed class Rating
{
    public Guid Id { get; init; }
    public Guid BookId { get; init; }
    public int Score { get; init; }
    public string? Comment { get; init; }
    public DateTime CreatedAt { get; init; }
}
