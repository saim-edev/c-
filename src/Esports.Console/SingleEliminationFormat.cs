// Lose once and you are out.
//
// Inherits the same shared validation as RoundRobinFormat, then adds one
// rule of its own that no other format needs.

public class SingleEliminationFormat : TournamentFormat
{
    public override string Name => "Single elimination";

    protected override List<Match> BuildFixtures(IReadOnlyList<Team> teams)
    {
        // The "at least 2 teams" check already ran, in the base class.
        // This rule belongs HERE and not in the base, because it is not
        // true of tournaments generally - a league with 3 teams is fine.
        // Only a knockout leaves somebody with nobody to play.
        //
        // Real tournaments handle odd counts with byes. Refusing keeps this
        // readable, and an honest limitation beats a silently wrong bracket.
        if (teams.Count % 2 != 0)
        {
            throw new ArgumentException(
                $"{Name} needs an even number of teams, got {teams.Count}");
        }

        List<Match> matches = new List<Match>();

        // Seeded pairing: strongest plays weakest. The list is assumed to
        // be in seed order, so index 0 is the top seed.
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

        // NOTE: round ONE only. Who plays in the semi-final depends on who
        // wins the quarter-final, and none of these have been played.
        return matches;
    }
}
