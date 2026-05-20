using BookRatings.Features.Books;

namespace BookRatings.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static void Seed(AppDbContext context)
    {
        if (context.Books.Any()) return;

        var cleanCodeId = Guid.NewGuid();
        var pragmaticId = Guid.NewGuid();
        var dddId = Guid.NewGuid();
        var refactoringId = Guid.NewGuid();
        var mythicalManMonthId = Guid.NewGuid();

        var books = new List<Book>
        {
            new()
            {
                Id = cleanCodeId,
                Title = "Clean Code",
                Author = "Robert C. Martin",
                PublishedYear = 2008,
                Ratings =
                [
                    new Rating { Id = Guid.NewGuid(), BookId = cleanCodeId, Score = 5, Comment = "A must-read.", CreatedAt = DateTime.UtcNow.AddDays(-30) },
                    new Rating { Id = Guid.NewGuid(), BookId = cleanCodeId, Score = 4, Comment = "Solid principles.", CreatedAt = DateTime.UtcNow.AddDays(-12) },
                    new Rating { Id = Guid.NewGuid(), BookId = cleanCodeId, Score = 5, CreatedAt = DateTime.UtcNow.AddDays(-2) }
                ]
            },
            new()
            {
                Id = pragmaticId,
                Title = "The Pragmatic Programmer",
                Author = "Andrew Hunt, David Thomas",
                PublishedYear = 1999,
                Ratings =
                [
                    new Rating { Id = Guid.NewGuid(), BookId = pragmaticId, Score = 5, Comment = "Timeless.", CreatedAt = DateTime.UtcNow.AddDays(-60) },
                    new Rating { Id = Guid.NewGuid(), BookId = pragmaticId, Score = 5, CreatedAt = DateTime.UtcNow.AddDays(-7) }
                ]
            },
            new()
            {
                Id = dddId,
                Title = "Domain-Driven Design",
                Author = "Eric Evans",
                PublishedYear = 2003,
                Ratings =
                [
                    new Rating { Id = Guid.NewGuid(), BookId = dddId, Score = 4, Comment = "Dense but rewarding.", CreatedAt = DateTime.UtcNow.AddDays(-90) },
                    new Rating { Id = Guid.NewGuid(), BookId = dddId, Score = 3, Comment = "Hard to digest at first.", CreatedAt = DateTime.UtcNow.AddDays(-15) }
                ]
            },
            new()
            {
                Id = refactoringId,
                Title = "Refactoring",
                Author = "Martin Fowler",
                PublishedYear = 1999,
                Ratings =
                [
                    new Rating { Id = Guid.NewGuid(), BookId = refactoringId, Score = 5, Comment = "Essential.", CreatedAt = DateTime.UtcNow.AddDays(-45) }
                ]
            },
            new()
            {
                Id = mythicalManMonthId,
                Title = "The Mythical Man-Month",
                Author = "Frederick P. Brooks Jr.",
                PublishedYear = 1975,
                Ratings = []
            }
        };

        context.Books.AddRange(books);
        context.SaveChanges();
    }
}
