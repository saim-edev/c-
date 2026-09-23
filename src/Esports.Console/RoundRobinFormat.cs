// Everyone plays everyone, once.
//
// `: TournamentFormat` means "inherit from that class". It brings along
// GenerateMatches() and the shared validation, already written.
//
// Compare with yesterday's `: ITournamentFormat`, which brought nothing and
// only demanded. A base class gives AND demands.

public class RoundRobinFormat : TournamentFormat
{
    // `override` = "I am supplying the body the base class left empty".
    // Leave this out and the file does not compile: the base declared Name
    // abstract, so a concrete subclass has to fill it.
    public override string Name => "Round robin";

    // Also `override`, and also `protected` - the access has to match what
    // the base declared. Nothing outside can call this directly, so nobody
    // can reach the fixtures without passing the validation first.
    protected override List<Match> BuildFixtures(IReadOnlyList<Team> teams)
    {
        List<Match> matches = new List<Match>();

        // The inner loop starts at i + 1, not 0. That one character is what
        // takes each pair once:
        //
        //        j=0  j=1  j=2  j=3
        //   i=0    -   X    X    X       X = a match
        //   i=1    -   -    X    X       - = skipped
        //   i=2    -   -    -    X
        //   i=3    -   -    -    -
        //
        // Starting j at 0 would give both "A plays B" and "B plays A", plus
        // "A plays A" down the diagonal.
        for (int i = 0; i < teams.Count; i++)
        {
            for (int j = i + 1; j < teams.Count; j++)
            {
                matches.Add(new Match(teams[i], teams[j]));
            }
        }

        return matches;
    }
}
