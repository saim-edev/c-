// A base class holding the parts EVERY format does the same way.
//
// `abstract` on the class means: you cannot create one of these directly.
// `new TournamentFormat()` is a compile error. It exists only to be
// inherited from - it is half a class, waiting for someone to finish it.
//
// WHY THIS EXISTS: both formats were opening with the same "at least 2
// teams" check. Two copies, and a third format would make three. Worse,
// nothing FORCED a new format to include it - someone would eventually
// write one and forget. An interface could not help: it holds no code.

public abstract class TournamentFormat : ITournamentFormat
{
    // `abstract` on a member = no body here, and every class that inherits
    // from this MUST supply one. Not a suggestion - a subclass that skips
    // it does not compile.
    //
    // There is no sensible default name for "a tournament format", so
    // demanding one is right.
    public abstract string Name { get; }

    // The shared part, written ONCE. Every format gets it automatically,
    // and cannot accidentally skip it.
    //
    // Read the shape carefully: this method does the common work, then
    // calls BuildFixtures - which has no body at this level. The base class
    // controls the ORDER; each subclass fills in the GAP.
    public List<Match> GenerateMatches(IReadOnlyList<Team> teams)
    {
        if (teams.Count < 2)
        {
            throw new ArgumentException(
                $"{Name} needs at least 2 teams, got {teams.Count}");
        }

        return BuildFixtures(teams);
    }

    // `protected` = visible to this class and anything inheriting from it,
    // but not to the outside world.
    //
    // That is deliberate. Callers should go through GenerateMatches, which
    // validates first. If this were public, a caller could skip straight to
    // BuildFixtures and bypass the check - the same "a guard is only as
    // good as its doors" problem that MatchScore had.
    protected abstract List<Match> BuildFixtures(IReadOnlyList<Team> teams);
}
