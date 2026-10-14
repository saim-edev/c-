// ============================================================================
//  THE ENTRY POINT
//
//  Unlike the console app's Program.cs, this one never reaches the bottom.
//  app.Run() at the end opens a port and WAITS, answering requests until
//  something kills the process.
//
//  The file has exactly two halves, split by builder.Build():
//
//    BEFORE Build()  -  a shopping list of things the app will need.
//                       Nothing is created yet.
//    AFTER Build()   -  what happens to every request that arrives.
// ============================================================================

// Esports.Core is a separate PROJECT (wired in the .csproj) and a separate
// NAMESPACE. The project reference says "I may use code from there"; this
// line says WHICH group of names. C# needs both - unlike JavaScript, you
// never import a file, you name a namespace.
using Esports.Core;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Esports.Api;


var builder = WebApplication.CreateBuilder(args);


// ---- HALF 1: what this app will need -------------------------------------
//
// These lines do NOT create anything. They add entries to a list, which
// Build() below turns into real objects. A shopping list, not the shopping.

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Register the DbContext. This is still HALF 1 - the shopping list - so
// nothing connects to anything yet. It records: "when someone asks for an
// EsportsDbContext, build one, pointed at Postgres, using this connection
// string."
//
// UseNpgsql is the provider from Day 8's package list. EF Core itself knows
// nothing about Postgres; this line is what teaches it the dialect.
builder.Services.AddDbContext<EsportsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Neon")));


// The dividing line.
var app = builder.Build();


// ---- HALF 2: what happens to each request --------------------------------

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();


// ---- THE ENDPOINTS -------------------------------------------------------

// Alive check. Answerable without a database or a login, which is why it is
// the first endpoint most APIs get.
app.MapGet("/health", () => new { status = "ok" });


// The league table, as JSON.
//
// EVERYTHING INSIDE THIS FUNCTION IS BUILT FRESH, PER REQUEST.
// Three users hitting this at once get three separate Tournament objects,
// three separate Lists, three separate everything. Nothing is shared, so
// nothing can be corrupted by another request running at the same moment.
// See docs/concepts/concurrency-and-threads.md.
app.MapGet("/standings", async (EsportsDbContext db) =>
{
    Tournament? league = await BuildTournamentAsync(db, new RoundRobinFormat());

    if (league is null)
    {
        return Results.BadRequest(new
        {
            error = "Not enough teams to run a league",
            hint = "POST /teams to add some"
        });
    }

    List<TeamStanding> table = StandingsTable.Build(league.Teams, league.Matches);

    return Results.Ok(table.Select((row, index) => new
    {
        position = index + 1,
        team = row.Team.Tag,
        name = row.Team.Name,
        played = row.Played,
        won = row.Won,
        drawn = row.Drawn,
        lost = row.Lost,
        gamesWon = row.GamesWon,
        gamesLost = row.GamesLost,
        gameDifference = row.GameDifference,
        points = row.Points
    }));
});


// The fixtures for a tournament, as JSON.
//
// NEW HERE: {format} in the path is a ROUTE PARAMETER. Whatever the caller
// puts there arrives as the `format` argument below. One endpoint, many
// answers:
//
//    GET /matches/league     ->  format == "league"
//    GET /matches/knockout   ->  format == "knockout"
//
// Nobody parses the URL by hand. The framework matches the pattern, pulls
// the piece out, and hands it over already typed as a string.
app.MapGet("/matches/{format}", async (string format, EsportsDbContext db) =>
{
    // Pick the format object from what the caller asked for.
    // ToLower() so "League", "LEAGUE" and "league" all work - the caller
    // should not have to guess our capitalisation.
    ITournamentFormat chosen = format.ToLower() switch
    {
        "league" => new RoundRobinFormat(),
        "knockout" => new SingleEliminationFormat(),

        // The catch-all arm. Returning null here rather than throwing,
        // because an unknown format is the CALLER's mistake, not a crash -
        // it should come back as a clear 400, which is what happens below.
        _ => null
    };

    if (chosen is null)
    {
        // Results.BadRequest sends HTTP 400 with a body explaining why.
        // Compare with the 404 an unmatched path gets: 404 means "no such
        // endpoint", 400 means "right endpoint, wrong input".
        return Results.BadRequest(new
        {
            error = $"Unknown format '{format}'",
            allowed = new[] { "league", "knockout" }
        });
    }

    Tournament? tournament = await BuildTournamentAsync(db, chosen);

    if (tournament is null)
    {
        return Results.BadRequest(new
        {
            error = "Not enough teams",
            hint = "POST /teams to add some"
        });
    }

    return Results.Ok(tournament.Matches.Select(m => new
    {
        home = m.HomeTeam.Tag,
        away = m.AwayTeam.Tag,
        state = m.State.ToString(),
        score = m.Score?.ToString(),
        winner = m.Winner?.Tag
    }));
});

// Can we actually reach the database?
//
// This uses the RAW Postgres driver on purpose - no EF Core yet. Everything
// EF Core does later sits on top of exactly this, so it is worth seeing once
// before it gets hidden.
//
// NOTE THE `async` AND THE `await`s. This is the first real use of them, and
// the reason is the whole of docs/concepts/concurrency-and-threads.md: the
// database is another computer, the answer takes tens of milliseconds, and a
// thread that sits and waits is a thread nobody else can use. `await` hands
// the thread back to the pool until the answer arrives.
app.MapGet("/db-check", async (IConfiguration config) =>
{
    // IConfiguration is handed to us. We never built it - the framework did,
    // at startup, by merging appsettings.json, environment variables and
    // user-secrets into one lookup. Asking for it as a parameter is how you
    // get hold of it. (That mechanism has a name - dependency injection -
    // and it arrives properly on Day 10.)
    string? connectionString = config.GetConnectionString("Neon");

    if (connectionString is null)
    {
        return Results.Problem("No connection string found. Run: dotnet user-secrets set \"ConnectionStrings:Neon\" \"...\"");
    }

    try
    {
        // `await using` = open it, and guarantee it gets closed even if
        // something throws. A database connection is a real OS resource -
        // leaving it open leaks it, and a pool has a limited number.
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();

        // SELECT version() is the cheapest possible question. It touches no
        // tables, so it proves the connection works and nothing else.
        await using var command = new NpgsqlCommand("SELECT version()", connection);
        object? serverVersion = await command.ExecuteScalarAsync();

        return Results.Ok(new
        {
            connected = true,
            database = connection.Database,
            server = serverVersion?.ToString()
        });
    }
    catch (Exception ex)
    {
        // Catching broadly here ON PURPOSE: this endpoint exists to report
        // whether the connection works, so a failure is the ANSWER, not a
        // crash. Everywhere else, catching Exception would be wrong.
        return Results.Problem($"{ex.GetType().Name}: {ex.Message}");
    }
});

// ---- TEAMS: the first endpoints backed by a real database ----

// CREATE a team.
//
// MapPost, not MapGet. GET asks for something; POST sends something and
// expects it to be created. The JSON body arrives as the first parameter,
// already turned into a C# object - you never parse JSON by hand.
//
// TWO parameters, and neither is passed by the caller:
//   CreateTeamRequest  <- comes from the request BODY
//   EsportsDbContext   <- comes from the framework, same as IConfiguration
app.MapPost("/teams", async (CreateTeamRequest request, EsportsDbContext db) =>
{
    // The Team constructor still enforces everything it did on Day 1. The
    // database has not replaced those rules, it stores their results.
    Team team = new Team(request.Name, request.Tag);

    // Add() does NOT touch the database. It tells the change tracker
    // "I intend to insert this". Nothing has left the process yet.
    db.Teams.Add(team);

    try
    {
        // THIS is the line that goes to Neon. Everything Add()-ed, Remove()-d
        // or modified since the last save goes in ONE transaction - all of it
        // lands, or none of it does.
        await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
        // The unique index on Tag rejected it. Note that the check lives in
        // the DATABASE - so this would fail even for someone typing SQL by
        // hand, not just for this program.
        //
        // 409 Conflict: the request was well-formed, but it clashes with
        // what already exists. Different from 400 (malformed input).
        return Results.Conflict(new { error = $"A team with tag '{request.Tag}' already exists" });
    }

    // team.Id was 0 a moment ago. Postgres generated the real one on insert
    // and EF wrote it back into the object - the aliasing from Day 2 working
    // for you: EF held a reference to this object and changed it.
    //
    // 201 Created, with a Location header saying where the new thing lives.
    return Results.Created($"/teams/{team.Id}", new TeamResponse(team.Id, team.Name, team.Tag));
});


// LIST the teams.
//
// `db.Teams` is the table as something you can query. The LINQ is Day 7's,
// except OrderBy becomes SQL and runs on the DATABASE - a thousand rows in
// the table, and only the ones you asked for cross the network.
app.MapGet("/teams", async (EsportsDbContext db) =>
{
    List<TeamResponse> teams = await db.Teams
        .OrderBy(t => t.Name)
        .Select(t => new TeamResponse(t.Id, t.Name, t.Tag))
        .ToListAsync();

    return Results.Ok(teams);
});

// ---- PLAYERS: the first endpoints across a relationship ----

// Add a player TO a team.
//
// The team id is in the URL, not the body. That is the usual REST shape for
// "a thing that belongs to another thing":
//
//    POST /teams/1/players   {"gamerTag":"Faker","rating":1847}
//         ^^^^^^^^           which team
//                 ^^^^^^^    what we are creating inside it
app.MapPost("/teams/{teamId:int}/players", async (
    int teamId,
    CreatePlayerRequest request,
    EsportsDbContext db) =>
{
    // Check the team exists BEFORE inserting. The foreign key would reject a
    // bad id anyway, but the caller would get a database error rather than a
    // clear 404. Fail in the place that can explain itself.
    //
    // AnyAsync asks the DATABASE "does a row match?" - it does not load the
    // team. SELECT EXISTS, not SELECT *.
    bool teamExists = await db.Teams.AnyAsync(t => t.Id == teamId);

    if (!teamExists)
    {
        return Results.NotFound(new { error = $"No team with id {teamId}" });
    }

    Player player = new Player(request.GamerTag, request.Rating, teamId);

    db.Players.Add(player);
    await db.SaveChangesAsync();

    return Results.Created(
        $"/teams/{teamId}/players/{player.Id}",
        new PlayerResponse(player.Id, player.GamerTag, player.Rating, player.TeamId));
});


// The players on a team.
//
// The Where runs on the DATABASE, using the index EF created on TeamId.
// With ten thousand players, only this team's rows cross the network.
app.MapGet("/teams/{teamId:int}/players", async (int teamId, EsportsDbContext db) =>
{
    List<PlayerResponse> players = await db.Players
        .Where(p => p.TeamId == teamId)
        .OrderByDescending(p => p.Rating)
        .Select(p => new PlayerResponse(p.Id, p.GamerTag, p.Rating, p.TeamId))
        .ToListAsync();

    return Results.Ok(players);
});


app.Run();


// ---- BUILDING A TOURNAMENT FROM THE DATABASE ----
//
// The four hardcoded teams are gone. Teams now come from Neon, which means
// POST /teams actually changes what /standings returns.
//
// STILL FAKE: the results. Matches are not persisted yet, so each request
// generates the fixtures and simulates scores with a seeded Random. That is
// the next step, and until then this function is half real.
//
// `async Task<Tournament?>` - async because it reads the database, and the
// `?` because there may not be enough teams to run anything.
static async Task<Tournament?> BuildTournamentAsync(
    EsportsDbContext db,
    ITournamentFormat format)
{
    // THE REAL READ. OrderBy runs on the database, not in C#.
    List<Team> teams = await db.Teams
        .OrderBy(t => t.Id)
        .ToListAsync();

    // Every format needs at least two teams, and single elimination needs an
    // even number. Checking here means the caller gets a clear answer rather
    // than an exception from deep inside Core.
    if (teams.Count < 2)
    {
        return null;
    }

    Tournament tournament = new Tournament("LCK Spring", format);

    foreach (Team team in teams)
    {
        tournament.Register(team);
    }

    tournament.GenerateMatches();

    // Seeded, so the same teams always produce the same scores. Without this
    // the table would reshuffle on every refresh and you could not tell a
    // real change from noise.
    Random rng = new Random(42);

    foreach (Match m in tournament.Matches)
    {
        m.Start();
        m.RecordResult(rng.Next(0, 4), rng.Next(0, 4));
    }

    return tournament;
}
