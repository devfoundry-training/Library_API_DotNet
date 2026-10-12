using Library.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Api.Data.Configurations;

/// <summary>The books table, matching db/schema.sql in the contract repo.</summary>
public class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> book)
    {
        book.ToTable("books", table =>
        {
            table.HasCheckConstraint("ck_books_year", "year BETWEEN 1450 AND 2100");
            table.HasCheckConstraint("ck_books_copies", "copies >= 0");
            table.HasCheckConstraint("ck_books_format", "format IN ('print', 'ebook', 'audio')");
            table.HasCheckConstraint("ck_books_length", "length > 0");
        });

        book.HasKey(b => b.Id);
        book.Property(b => b.Id).HasColumnName("id");
        book.Property(b => b.Title).HasColumnName("title").HasMaxLength(100).IsRequired();
        book.Property(b => b.Author).HasColumnName("author").HasMaxLength(100).IsRequired();
        book.Property(b => b.Year).HasColumnName("year");
        book.Property(b => b.Genre).HasColumnName("genre").HasMaxLength(50).IsRequired();
        book.Property(b => b.Copies).HasColumnName("copies");
        book.Property(b => b.Format).HasColumnName("format").HasMaxLength(10).IsRequired();
        book.Property(b => b.Length).HasColumnName("length");
    }
}
