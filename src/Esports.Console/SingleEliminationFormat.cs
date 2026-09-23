// Lose once and you are out.
//
// Fills the same contract as RoundRobinFormat, and does something completely
// different with it. Same input, same output type, different answer.

public class SingleEliminationFormat : ITournamentFormat
{
    public string Name => "Single elimination";

    public List<Match> GenerateMatches(IReadOnlyList<Team> teams)
    {
        if (teams.Count < 2)
        {
            throw new ArgumentException(
                $"Single elimination needs at least 2 teams, got {teams.Count}");
        }

        // An odd count means somebody has nobody to play. Real tournaments
        // handle this with byes; refusing it keeps this readable, and an
        // honest limitation beats a silently wrong bracket.
        if (teams.Count % 2 != 0)
        {
            throw new ArgumentException(
                $"Single elimination needs an even number of teams, got {teams.Count}");
        }

        List<Match> matches = new List<Match>();

        // Seeded pairing: strongest plays weakest. The list is assumed to be
        // in seed order, so index 0 is the top seed.
        //
        //   4 teams:          8 teams:
        //     0 v 3             0 v 7
        //     1 v 2             1 v 6
        //                       2 v 5
        //                       3 v 4
        //
        // Why: it keeps the best teams apart until later rounds. Pairing
        // 0 v 1 in round one would knock out a finalist immediately.
        int half = teams.Count / 2;

        for (int i = 0; i < half; i++)
        {
            matches.Add(new Match(teams[i], teams[teams.Count - 1 - i]));
        }

        // NOTE: this is round ONE only. Later rounds cannot be built yet -
        // who plays in the semi-final depends on who wins the quarter-final,
        // and none of these have been played. Advancing winners into the
        // next round comes later.
        return matches;
    }
}
