// An enum is a fixed, named set of options.
//
// The alternative was one bool per state - but four bools give SIXTEEN
// combinations when only four are legal, so nothing would stop a match being
// Completed and InProgress at once. A string would allow typos to compile.
//
// With an enum there are exactly five possible values, the compiler knows all
// of them, and MatchState.InProgres (typo) is a build error rather than a bug
// you find in production.

public enum MatchState
{
    // Fixture exists, nobody has played yet.
    Scheduled,

    // Currently being played.
    InProgress,

    // Played to a finish; there is a score.
    Completed,

    // One team did not show up. Ends the match, but there is no real score.
    Forfeited,

    // Called off. Never played, never will be.
    Cancelled
}
