// A match pairs two teams. It may or may not have been played yet.

public class Match
{
    // ---- DATA ----

    // These hold ARROWS to Team objects, not copies of them. If a player is
    // added to HomeTeam elsewhere in the program, this match sees it too -
    // because there is only one Team object, with two names pointing at it.
    public Team HomeTeam { get; private set; }
    public Team AwayTeam { get; private set; }

    // The `?` means "this might be nothing".
    //
    // A match that has not been played has no score at all. 0-0 would be a
    // lie - that is a real result, a draw. `MatchScore?` lets the field
    // genuinely hold nothing, and makes the compiler force you to check
    // before you read it.
    public MatchScore? Score { get; private set; }

    // ---- CONSTRUCTOR ----

    public Match(Team homeTeam, Team awayTeam)
    {
        // A team cannot play itself. Checking here means a nonsensical
        // Match can never exist, rather than being caught later somewhere.
        if (ReferenceEquals(homeTeam, awayTeam))
        {
            throw new ArgumentException("A team cannot play itself");
        }

        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        Score = null;          // explicitly: not played yet
    }

    // ---- BEHAVIOUR ----

    // Reading a property that is just `Score != null` gives every other part
    // of the program one clear question to ask, instead of each one doing
    // its own null check and some of them getting it wrong.
    public bool HasBeenPlayed => Score != null;

    public void RecordResult(int homeScore, int awayScore)
    {
        if (HasBeenPlayed)
        {
            throw new InvalidOperationException(
                $"{HomeTeam.Tag} vs {AwayTeam.Tag} already finished {Score}");
        }

        // Create() rather than `new` - it rejects negative numbers, so an
        // invalid score cannot get in here.
        Score = MatchScore.Create(homeScore, awayScore);
    }

    // Who won. Returns a Team? because there may be no winner: the match
    // might not have been played, or it might have been a draw.
    public Team? Winner
    {
        get
        {
            if (Score == null || Score.IsDraw)
            {
                return null;
            }

            return Score.HomeWon ? HomeTeam : AwayTeam;
        }
    }

    public override string ToString()
    {
        if (Score == null)
        {
            return $"{HomeTeam.Tag} vs {AwayTeam.Tag} (not played)";
        }

        return $"{HomeTeam.Tag} {Score} {AwayTeam.Tag}";
    }
}
