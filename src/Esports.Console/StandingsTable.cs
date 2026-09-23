// Turns a list of played matches into a league table.
//
// This file was first written with nested loops and six manually incremented
// counters - 75 lines. The version below does the same work in about 20, and
// says what it means rather than how to do it.

public static class StandingsTable
{
    public static List<TeamStanding> Build(
        IReadOnlyList<Team> teams,
        IReadOnlyList<Match> matches)
    {
        // Work out the finished matches ONCE rather than re-filtering per
        // team. Note the .ToList() - see the comment on Played below for
        // why that matters.
        List<Match> finished = matches
            .Where(m => m.State == MatchState.Completed && m.Score != null)
            .ToList();

        return teams
            .Select(team => BuildRow(team, finished))
            .OrderByDescending(row => row.Points)
            .ThenByDescending(row => row.GameDifference)
            .ThenByDescending(row => row.GamesWon)
            .ToList();
    }

    private static TeamStanding BuildRow(Team team, List<Match> finished)
    {
        // Every finished match this team appeared in, with the score already
        // flipped so "us" and "them" are from THIS team's point of view.
        // Doing that once here is what keeps everything below simple.
        List<(int Us, int Them)> results = finished
            .Where(m => ReferenceEquals(m.HomeTeam, team)
                     || ReferenceEquals(m.AwayTeam, team))
            .Select(m => ReferenceEquals(m.HomeTeam, team)
                ? (Us: m.Score!.Home, Them: m.Score!.Away)
                : (Us: m.Score!.Away, Them: m.Score!.Home))
            .ToList();

        return new TeamStanding(
            Team: team,
            Played: results.Count,
            Won: results.Count(r => r.Us > r.Them),
            Drawn: results.Count(r => r.Us == r.Them),
            Lost: results.Count(r => r.Us < r.Them),
            GamesWon: results.Sum(r => r.Us),
            GamesLost: results.Sum(r => r.Them));
    }

    public static void Print(IReadOnlyList<TeamStanding> rows)
    {
        Console.WriteLine("  #  Team   P   W   D   L   Games    GD  Pts");
        Console.WriteLine("  ------------------------------------------");

        for (int i = 0; i < rows.Count; i++)
        {
            Console.WriteLine($"  {i + 1,-2} {rows[i]}");
        }
    }
}
