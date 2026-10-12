using Library.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Library.Api.Data.Configurations;

/// <summary>The loans table, matching db/schema.sql in the contract repo.</summary>
public class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> loan)
    {
        loan.ToTable("loans", table =>
            table.HasCheckConstraint("ck_loans_returned", "returned IS NULL OR returned >= borrowed"));

        loan.HasKey(l => l.Id);
        loan.Property(l => l.Id).HasColumnName("id");
        loan.Property(l => l.BookId).HasColumnName("book_id");
        loan.Property(l => l.MemberId).HasColumnName("member_id");
        loan.Property(l => l.Borrowed).HasColumnName("borrowed");
        loan.Property(l => l.Returned).HasColumnName("returned");

        // Restrict: a book or member with loans is never deleted by a cascade.
        loan.HasOne(l => l.Book).WithMany(b => b.Loans).HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);
        loan.HasOne(l => l.Member).WithMany(m => m.Loans).HasForeignKey(l => l.MemberId)
            .OnDelete(DeleteBehavior.Restrict);

        loan.HasIndex(l => l.BookId).HasDatabaseName("ix_loans_book_id");
        loan.HasIndex(l => l.MemberId).HasDatabaseName("ix_loans_member_id");
    }
}
