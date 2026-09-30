// One row of a league table.
//
// A record, not a class. A standing row IS its contents - two rows with the
// same numbers are the same row. Contrast with Team, which has an identity
// that survives its values changing.
//
// It is a snapshot: recalculated from the matches whenever it is asked for,
// never stored and updated. That means it cannot drift out of sync with the
// results, which is a whole class of bug avoided.

public record TeamStanding(
    Team Team,
    int Played,
    int Won,
    int Drawn,
    int Lost,
    int GamesWon,
    int GamesLost)
{
    // Football scoring: 3 for a win, 1 for a draw, 0 for a loss.
    public int Points => (Won * 3) + Drawn;

    // Games won minus games lost. The usual first tie-break when two teams
    // are level on points.
    public int GameDifference => GamesWon - GamesLost;

    public override string ToString() =>
        $"{Team.Tag,-5} {Played,2} {Won,3} {Drawn,3} {Lost,3} " +
        $"{GamesWon,4}-{GamesLost,-3} {GameDifference,4} {Points,4}";
}
