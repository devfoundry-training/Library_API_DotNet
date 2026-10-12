using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Library.Api.Data;

public static class DatabaseSetup
{
    /// <summary>Registers LibraryDbContext for the database DATABASE_URL points at.</summary>
    public static IServiceCollection AddLibraryDatabase(this IServiceCollection services, DatabaseUrl database)
    {
        if (database.Provider == DatabaseProvider.PostgreSql)
        {
            services.AddDbContext<LibraryDbContext, PostgreSqlLibraryDbContext>(options =>
                options.UseNpgsql(database.ConnectionString));
        }
        else
        {
            services.AddDbContext<LibraryDbContext, SqliteLibraryDbContext>(options =>
                options.UseSqlite(database.ConnectionString));
        }

        services.AddScoped<LibrarySeeder>();
        return services;
    }

    /// <summary>Runs the migrations, then loads the sample data if the books table is empty.</summary>
    public static async Task PrepareLibraryDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        await db.Database.MigrateAsync();
        await scope.ServiceProvider.GetRequiredService<LibrarySeeder>().SeedIfEmptyAsync();
    }
}

// Used only by `dotnet ef migrations add`. No database is opened.

public sealed class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<SqliteLibraryDbContext>
{
    public SqliteLibraryDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<SqliteLibraryDbContext>().UseSqlite("Data Source=library.db").Options);
}

public sealed class PostgreSqlDesignTimeFactory : IDesignTimeDbContextFactory<PostgreSqlLibraryDbContext>
{
    public PostgreSqlLibraryDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<PostgreSqlLibraryDbContext>()
            .UseNpgsql("Host=localhost;Database=library;Username=library;Password=library").Options);
}
