// A `record` is a class where equality is based on CONTENTS, not identity.
// Use a record when the thing IS its contents: a score, a date range, a
// money amount, an ID. Use a plain class when the thing has an identity
// that outlives its current values - a Player, a Team, a Tournament.
//
// NOTE ON SHAPE: this was originally written as a one-line positional record:
//
//     public record MatchScore(int Home, int Away);
//
// which is shorter and reads better. It had a hole. A positional record's
// constructor is PUBLIC, so `new MatchScore(-1, 3)` sailed straight past
// Create() and built a score with a margin of 4 and 2 games played:
//
//     via Create():  refused: Scores cannot be negative: -1-3
//     via new:       built: -1-3  Margin=4  GamesPlayed=2
//
// A guard only works if it is the ONLY way in. So the constructor is now
// private and Create() is the single door. The cost is real and worth
// knowing: `with` no longer works from outside, because that would be
// another way to reach an invalid value. WithHome/WithAway below replace it
// and re-validate.

public record MatchScore
{
    // Get-only, set in the private constructor. Nothing outside can change
    // these after construction.
    public int Home { get; }
    public int Away { get; }

    // PRIVATE. This is the whole fix - it closes the door that `new` left
    // open. Only Create() below can reach it.
    private MatchScore(int home, int away)
    {
        Home = home;
        Away = away;
    }

    // The single way to build a MatchScore. `static` = belongs to the type,
    // so it is called as MatchScore.Create(...), not on an instance.
    public static MatchScore Create(int home, int away)
    {
        if (home < 0 || away < 0)
        {
            throw new ArgumentException($"Scores cannot be negative: {home}-{away}");
        }

        return new MatchScore(home, away);
    }

    // Replacements for `with`, which cannot be used from outside any more.
    // Both route through Create(), so they re-validate - you cannot edit
    // your way to an invalid score either.
    public MatchScore WithHome(int home) => Create(home, Away);

    public MatchScore WithAway(int away) => Create(Home, away);

    // ---- what a score knows about itself ----
    //
    // `=>` means "this property is computed, here is the expression".
    // Without this type, all of it would be loose helper methods taking
    // (int home, int away), copied into every file that reads a result.

    public bool IsDraw => Home == Away;

    public bool HomeWon => Home > Away;

    public bool AwayWon => Away > Home;

    // How decisive the win was. Math.Abs strips the minus sign, so 1-3 and
    // 3-1 both give 2.
    public int Margin => Math.Abs(Home - Away);

    // Total games in the series - a Bo5 ending 3-1 took 4 games.
    public int GamesPlayed => Home + Away;

    // `override` = replace the version this type inherited. EVERY type in C#
    // gets a free ToString(); a record's generated version lists every public
    // property, which got noisy once the computed ones above were added.
    public override string ToString() => $"{Home}-{Away}";
}
