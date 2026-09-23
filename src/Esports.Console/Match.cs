// A match pairs two teams and moves through a fixed lifecycle.

public class Match
{
    // ---- DATA ----

    // These hold ARROWS to Team objects, not copies of them. If a player is
    // added to HomeTeam elsewhere in the program, this match sees it too -
    // because there is only one Team object, with two names pointing at it.
    public Team HomeTeam { get; private set; }
    public Team AwayTeam { get; private set; }

    // Exactly one of five values, always. Not four bools (which would allow
    // sixteen combinations, most of them nonsense) and not a string (where a
    // typo compiles fine).
    public MatchState State { get; private set; }

    // The `?` means "this might be nothing". A match that has not been played
    // has no score, and 0-0 would be a lie - that is a real result, a draw.
    public MatchScore? Score { get; private set; }

    // Set only on a forfeit, where there is a winner but no real score.
    public Team? ForfeitWinner { get; private set; }

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
        State = MatchState.Scheduled;   // every match starts here
        Score = null;
    }

    // ---- TRANSITIONS ----
    //
    // Not every move between states is legal. The legal ones are:
    //
    //            ┌──────────────► Cancelled
    //            │
    //   Scheduled ──► InProgress ──► Completed
    //            │         │
    //            └─────────┴──────► Forfeited
    //
    // Each method below enforces one arrow. Anything else throws, so the
    // object can never reach a state it has no legal path to.

    public void Start()
    {
        if (State != MatchState.Scheduled)
        {
            throw new InvalidOperationException(
                $"Cannot start a match that is {State}");
        }

        State = MatchState.InProgress;
    }

    public void RecordResult(int homeScore, int awayScore)
    {
        if (State != MatchState.InProgress)
        {
            throw new InvalidOperationException(
                $"Cannot record a result for a match that is {State}");
        }

        // Create() rather than `new` - it rejects negative numbers, so an
        // invalid score cannot get in here.
        Score = MatchScore.Create(homeScore, awayScore);
        State = MatchState.Completed;
    }

    public void Forfeit(Team winner)
    {
        if (State != MatchState.Scheduled && State != MatchState.InProgress)
        {
            throw new InvalidOperationException(
                $"Cannot forfeit a match that is {State}");
        }

        if (!ReferenceEquals(winner, HomeTeam) && !ReferenceEquals(winner, AwayTeam))
        {
            throw new ArgumentException($"{winner.Tag} is not playing in this match");
        }

        ForfeitWinner = winner;
        State = MatchState.Forfeited;
    }

    public void Cancel()
    {
        if (State != MatchState.Scheduled)
        {
            throw new InvalidOperationException(
                $"Cannot cancel a match that is {State}");
        }

        State = MatchState.Cancelled;
    }

    // ---- QUESTIONS THE MATCH CAN ANSWER ----

    // A match is over if it reached any state it cannot leave.
    public bool IsFinished =>
        State == MatchState.Completed
        || State == MatchState.Forfeited
        || State == MatchState.Cancelled;

    // Who won. Null is a real answer here: not played yet, a draw, cancelled.
    public Team? Winner
    {
        get
        {
            if (State == MatchState.Forfeited)
            {
                return ForfeitWinner;
            }

            if (State != MatchState.Completed || Score == null || Score.IsDraw)
            {
                return null;
            }

            return Score.HomeWon ? HomeTeam : AwayTeam;
        }
    }

    public override string ToString()
    {
        // A `switch expression` picks one value out of several cases.
        // `_` is the catch-all arm. Reads top to bottom, first match wins.
        string detail = State switch
        {
            MatchState.Scheduled  => "not played yet",
            MatchState.InProgress => "live now",
            MatchState.Completed  => $"{Score}",
            MatchState.Forfeited  => $"forfeit, {ForfeitWinner?.Tag} advances",
            MatchState.Cancelled  => "cancelled",
            _                     => "unknown"
        };

        return $"{HomeTeam.Tag} vs {AwayTeam.Tag} [{State}] {detail}";
    }
}
