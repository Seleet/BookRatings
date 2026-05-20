using BookRatings.Features.Books;
using Microsoft.EntityFrameworkCore;

namespace BookRatings.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Rating> Ratings => Set<Rating>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).IsRequired();
            b.Property(x => x.Author).IsRequired();
            b.Ignore(x => x.AverageRating);
            b.HasMany(x => x.Ratings)
                .WithOne()
                .HasForeignKey(r => r.BookId);
        });

        modelBuilder.Entity<Rating>(r =>
        {
            r.HasKey(x => x.Id);
        });
    }
}
