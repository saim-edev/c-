// An interface is a CONTRACT. It lists what something must be able to do,
// and contains no code to do it.
//
// This one says: whatever you are, you must have a Name, and you must be
// able to turn a list of teams into a list of matches. How you do that is
// entirely your business.
//
// The `I` prefix is a C# naming convention for interfaces. Nothing enforces
// it, but every codebase you meet will use it.
//
// WHY THIS EXISTS: without it, Tournament.GenerateMatches() would need an
// `if` per format, and Tournament would have to know about every format that
// will ever be written. With it, Tournament holds "some format" and never
// learns which one.

public interface ITournamentFormat
{
    // No body. An interface declares, it does not implement.
    string Name { get; }

    // Takes the teams, hands back the fixtures.
    //
    // It takes IReadOnlyList<Team> rather than List<Team> deliberately: a
    // format works out who plays who, it has no business adding or removing
    // teams. Ask for the narrowest thing that does the job.
    List<Match> GenerateMatches(IReadOnlyList<Team> teams);
}
