// A `record` is a class where equality is based on CONTENTS, not identity.
// Use a record when the thing IS its contents: a score, a date range, a
// money amount, an ID. Use a plain class when the thing has an identity
// that outlives its current values - a Player, a Team, a Tournament.
//
// The two values in brackets are the data. Everything in the body below is
// the behaviour that BELONGS to a score. Without this type, all of it would
// be loose helper methods taking (int home, int away) and getting copied
// into every file that ever looks at a result.

public record MatchScore(int Home, int Away)
{
    // `=>` here means "this property is computed, here is the expression".
    // Same as writing { get { return Home == Away; } } but shorter.
    public bool IsDraw => Home == Away;

    public bool HomeWon => Home > Away;

    public bool AwayWon => Away > Home;

    // How decisive the win was. Useful later for seeding and tie-breaks.
    // Math.Abs strips the minus sign, so 1-3 and 3-1 both give 2.
    public int Margin => Math.Abs(Home - Away);

    // Total games played in the series - a Bo5 that ends 3-1 took 4 games.
    public int GamesPlayed => Home + Away;

    // A guard: a score cannot be negative. Throwing here means an invalid
    // MatchScore can never exist anywhere in the program, not even briefly.
    public static MatchScore Create(int home, int away)
    {
        if (home < 0 || away < 0)
        {
            throw new ArgumentException($"Scores cannot be negative: {home}-{away}");
        }

        return new MatchScore(home, away);
    }

    // `override` = replace the version this type inherited. EVERY type in C#
    // gets a free ToString(); a record's version lists every public property,
    // which got noisy once the computed ones above were added. This replaces
    // it with the only form anyone actually wants to read.
    public override string ToString() => $"{Home}-{Away}";
}
