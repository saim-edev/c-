// Everyone plays everyone, once.
//
// `: ITournamentFormat` means "this class fills that contract". The compiler
// now checks that Name and GenerateMatches both exist with exactly the right
// shape. Delete either one and this file stops compiling.

public class RoundRobinFormat : ITournamentFormat
{
    public string Name => "Round robin";

    public List<Match> GenerateMatches(IReadOnlyList<Team> teams)
    {
        if (teams.Count < 2)
        {
            throw new ArgumentException(
                $"Round robin needs at least 2 teams, got {teams.Count}");
        }

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
