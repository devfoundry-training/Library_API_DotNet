using Microsoft.EntityFrameworkCore;

namespace Library.Api.Data;

/// <summary>
/// Loads the sample data from Data/seed.sql (a copy of db/seed.sql from the contract repo):
/// 20 books, 9 members and 12 loans with their fixed ids.
/// </summary>
public sealed class LibrarySeeder(LibraryDbContext db)
{
    private static readonly Lazy<string> SeedSql = new(ReadSeedSql);

    // Inserting explicit ids never moves a PostgreSQL sequence, so move each one past the highest id.
    private static readonly string[] FixPostgreSqlSequences =
    [
        "SELECT setval(pg_get_serial_sequence('books', 'id'), COALESCE(MAX(id), 0) + 1, false) FROM books",
        "SELECT setval(pg_get_serial_sequence('members', 'id'), COALESCE(MAX(id), 0) + 1, false) FROM members",
        "SELECT setval(pg_get_serial_sequence('loans', 'id'), COALESCE(MAX(id), 0) + 1, false) FROM loans",
    ];

    public async Task SeedIfEmptyAsync()
    {
        if (await db.Books.AnyAsync())
        {
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync();
        await LoadSampleDataAsync();
        await transaction.CommitAsync();
    }

    /// <summary>
    /// Empties the three tables and loads the sample data again, in one transaction, so the next
    /// ids are book 21, member 110 and loan 13. Only reachable through POST /test/reset.
    /// </summary>
    public async Task ResetAsync()
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        if (db.Database.IsNpgsql())
        {
            await db.Database.ExecuteSqlRawAsync("TRUNCATE loans, members, books RESTART IDENTITY");
        }
        else
        {
            // sqlite_sequence holds the AUTOINCREMENT counters; without this the next book would be 22.
            await db.Database.ExecuteSqlRawAsync(
                "DELETE FROM loans; DELETE FROM members; DELETE FROM books; " +
                "DELETE FROM sqlite_sequence WHERE name IN ('loans', 'members', 'books');");
        }
        await LoadSampleDataAsync();
        await transaction.CommitAsync();
        db.ChangeTracker.Clear();
    }

    private async Task LoadSampleDataAsync()
    {
        await db.Database.ExecuteSqlRawAsync(SeedSql.Value);
        if (db.Database.IsNpgsql())
        {
            foreach (var sql in FixPostgreSqlSequences)
            {
                await db.Database.ExecuteSqlRawAsync(sql);
            }
        }
    }

    private static string ReadSeedSql()
    {
        using var stream = typeof(LibrarySeeder).Assembly.GetManifestResourceStream("Library.Api.Data.seed.sql")
            ?? throw new InvalidOperationException("seed.sql is missing from the assembly");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
