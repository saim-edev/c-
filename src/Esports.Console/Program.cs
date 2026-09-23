// ---- two teams ----

Team t1 = new Team("T1", "T1");
t1.AddPlayer(new Player("Faker", 1847));
t1.AddPlayer(new Player("Gumayusi", 1791));

Team gen = new Team("Gen.G", "GEN");
gen.AddPlayer(new Player("Chovy", 1792));
gen.AddPlayer(new Player("Peyz", 1760));


// ---- a match walks through its lifecycle ----

Match final = new Match(t1, gen);

Console.WriteLine("--- the happy path ---");
Console.WriteLine(final);

final.Start();
Console.WriteLine(final);

final.RecordResult(3, 1);
Console.WriteLine(final);

Console.WriteLine($"Finished? {final.IsFinished}   Winner: {final.Winner?.Name ?? "-"}");


// ---- illegal moves are refused, not silently allowed ----

Console.WriteLine();
Console.WriteLine("--- illegal transitions ---");

TryThis("start a finished match", () => final.Start());
TryThis("cancel a finished match", () => final.Cancel());
TryThis("record a result twice", () => final.RecordResult(2, 0));

Match early = new Match(t1, gen);
TryThis("record a result before starting", () => early.RecordResult(2, 0));


// ---- the other two endings ----

Console.WriteLine();
Console.WriteLine("--- forfeit and cancel ---");

Match noShow = new Match(t1, gen);
noShow.Start();
noShow.Forfeit(t1);
Console.WriteLine(noShow);
Console.WriteLine($"Winner: {noShow.Winner?.Name ?? "-"}");

Match calledOff = new Match(t1, gen);
calledOff.Cancel();
Console.WriteLine(calledOff);
Console.WriteLine($"Winner: {calledOff.Winner?.Name ?? "-"}");


// ---- a draw still has no winner ----

Console.WriteLine();
Console.WriteLine("--- a draw ---");
Match group = new Match(t1, gen);
group.Start();
group.RecordResult(1, 1);
Console.WriteLine(group);
Console.WriteLine($"Winner: {group.Winner?.Name ?? "nobody - it was a draw"}");


// A small helper so each attempt below reads as one line.
// `Action` is "a thing that can be run and returns nothing".
static void TryThis(string what, Action attempt)
{
    try
    {
        attempt();
        Console.WriteLine($"  allowed : {what}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  refused : {what} -> {ex.Message}");
    }
}
