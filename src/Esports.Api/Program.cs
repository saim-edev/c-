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


var builder = WebApplication.CreateBuilder(args);


// ---- HALF 1: what this app will need -------------------------------------
//
// These lines do NOT create anything. They add entries to a list, which
// Build() below turns into real objects. A shopping list, not the shopping.

builder.Services.AddControllers();
builder.Services.AddOpenApi();


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
app.MapGet("/standings", () =>
{
    Tournament league = BuildTournament(new RoundRobinFormat());

    // StandingsTable, Tournament, Team, Match all come from Esports.Core -
    // the exact same classes the console app uses. Only the front door
    // differs: that one prints, this one returns JSON.
    List<TeamStanding> table = StandingsTable.Build(league.Teams, league.Matches);

    // Shape the answer rather than returning TeamStanding directly.
    //
    // WHY: TeamStanding holds a whole Team object, which holds its players,
    // which hold their ratings. Returning it would send the entire object
    // graph down the wire - far more than a table needs, and it would change
    // shape every time the domain changes. The caller should not be coupled
    // to the internals of our classes.
    //
    // `Select` is map, from Day 7. Each row becomes a small flat object.
    return table.Select((row, index) => new
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
    });
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
app.MapGet("/matches/{format}", (string format) =>
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

    Tournament tournament = BuildTournament(chosen);

    return Results.Ok(tournament.Matches.Select(m => new
    {
        home = m.HomeTeam.Tag,
        away = m.AwayTeam.Tag,
        state = m.State.ToString(),
        score = m.Score?.ToString(),
        winner = m.Winner?.Tag
    }));
});


app.Run();


// ---- BUILDING THE DATA ---------------------------------------------------
//
// Hardcoded, on purpose, and temporary. There is no database yet, so every
// request builds the same four teams and replays the same six matches.
//
// The Random is seeded, so the results are identical every time - otherwise
// refreshing the page would show a different table and you could not tell a
// real change from noise.
//
// This whole function disappears around Day 10, replaced by a database read.
//
// It now takes the format as an argument rather than hardcoding RoundRobin,
// so the same builder serves both endpoints. Tournament never learns which
// format it got - the Day 5 lesson, still holding.
static Tournament BuildTournament(ITournamentFormat format)
{
    Team t1 = MakeTeam("T1", "T1", 1847, 1791, 1823);
    Team gen = MakeTeam("Gen.G", "GEN", 1792, 1760, 1744);
    Team hle = MakeTeam("Hanwha", "HLE", 1755, 1730, 1718);
    Team dk = MakeTeam("Dplus", "DK", 1740, 1712, 1699);

    Tournament league = new Tournament("LCK Spring", format);

    foreach (Team team in new[] { t1, gen, hle, dk })
    {
        league.Register(team);
    }

    league.GenerateMatches();

    // Seeded, so the same "random" results every time. Without this,
    // refreshing the page would show a different table and you could not
    // tell a real change from noise.
    Random rng = new Random(42);

    foreach (Match m in league.Matches)
    {
        m.Start();
        m.RecordResult(rng.Next(0, 4), rng.Next(0, 4));
    }

    return league;
}

static Team MakeTeam(string name, string tag, params int[] ratings)
{
    Team team = new Team(name, tag);

    for (int i = 0; i < ratings.Length; i++)
    {
        team.AddPlayer(new Player($"{tag}-p{i + 1}", ratings[i]));
    }

    return team;
}
