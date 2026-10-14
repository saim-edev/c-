using Esports.Core;
using Microsoft.EntityFrameworkCore;

namespace Esports.Api;

// THE BRIDGE between your C# classes and the database.
//
// It answers three questions:
//   1. WHICH classes become tables?        -> the DbSet properties below
//   2. HOW do they map to columns?         -> OnModelCreating
//   3. WHAT have I changed since loading?  -> the change tracker, built in
//
// It lives in Esports.Api, not Esports.Core, on purpose. Core has no database
// packages at all - try adding `using Microsoft.EntityFrameworkCore;` to a
// file in there and it will not compile. The domain does not know databases
// exist; this class is where that knowledge is allowed to live.
//
// `: DbContext` - inheriting, like Day 6. DbContext is a base class that
// already knows how to open connections, track changes, generate SQL and
// save. You supply the holes: which types, and how they map.
public class EsportsDbContext : DbContext
{
    // The constructor takes its settings from outside rather than building
    // them. Which connection string, which database provider - none of that
    // is decided here. Same pattern as Tournament taking an
    // ITournamentFormat on Day 5: hand the dependency in, do not reach for it.
    public EsportsDbContext(DbContextOptions<EsportsDbContext> options)
        : base(options)
    {
    }

    // ---- WHICH CLASSES BECOME TABLES ----
    //
    // One DbSet per table. THIS LINE is what makes Team a table - without it
    // EF Core has no idea the class exists.
    //
    // Read `DbSet<Team>` as "the teams table, as a thing you can query".
    // The LINQ from Day 7 works on it, except Where and OrderBy become SQL
    // and run on the database rather than in C#.
    public DbSet<Team> Teams => Set<Team>();

    // ---- HOW THEY MAP TO COLUMNS ----
    //
    // Called once, when EF first works out the shape of the database.
    // Most of it is convention: a property called Id becomes the primary key,
    // a string becomes text, an int becomes integer. You only write what the
    // convention gets wrong or cannot know.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(team =>
        {
            // Spelling out the key rather than relying on the naming
            // convention. One line, and now nobody has to know the rule.
            team.HasKey(t => t.Id);

            // Without a length, Postgres gets `text` - unlimited. Not wrong,
            // but a cap is a cheap sanity check at the storage layer.
            team.Property(t => t.Name).HasMaxLength(100).IsRequired();
            team.Property(t => t.Tag).HasMaxLength(10).IsRequired();

            // No two teams may share a tag. Note WHERE this lives: in the
            // database, not in C#. A C# check only stops THIS program; a
            // database constraint stops everything, including someone typing
            // SQL by hand. Same instinct as Day 1 - put the rule where it
            // cannot be bypassed.
            team.HasIndex(t => t.Tag).IsUnique();

            // Team also holds `private List<Player> _players`. EF Core cannot
            // see it - there is no public property exposing it, which has
            // been true since Day 2. So nothing to configure here yet.
            // Relationships are the next step; one new idea at a time.
        });
    }
}
