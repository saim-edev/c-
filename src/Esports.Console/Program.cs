// ---- two teams ----

Team t1 = new Team("T1", "T1");
t1.AddPlayer(new Player("Faker", 1847));
t1.AddPlayer(new Player("Gumayusi", 1791));
t1.AddPlayer(new Player("Keria", 1823));

Team gen = new Team("Gen.G", "GEN");
gen.AddPlayer(new Player("Chovy", 1792));
gen.AddPlayer(new Player("Peyz", 1760));
gen.AddPlayer(new Player("Lehends", 1744));


// ---- a match that has not been played yet ----

Match final = new Match(t1, gen);

Console.WriteLine("--- before kickoff ---");
Console.WriteLine(final);
Console.WriteLine($"Played? {final.HasBeenPlayed}");
Console.WriteLine($"Winner: {final.Winner?.Name ?? "nobody yet"}");


// ---- play it ----

final.RecordResult(3, 1);

Console.WriteLine();
Console.WriteLine("--- after the match ---");
Console.WriteLine(final);
Console.WriteLine($"Played? {final.HasBeenPlayed}");
Console.WriteLine($"Winner: {final.Winner?.Name ?? "nobody yet"}");
Console.WriteLine($"Margin: {final.Score?.Margin}");


// ---- the rules defend themselves ----

Console.WriteLine();
Console.WriteLine("--- what the rules refuse ---");

try
{
    final.RecordResult(2, 0);
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"refused: {ex.Message}");
}

try
{
    Match nonsense = new Match(t1, t1);
}
catch (ArgumentException ex)
{
    Console.WriteLine($"refused: {ex.Message}");
}


// ---- a draw has no winner ----

Match groupGame = new Match(t1, gen);
groupGame.RecordResult(1, 1);

Console.WriteLine();
Console.WriteLine("--- a draw ---");
Console.WriteLine(groupGame);
Console.WriteLine($"Winner: {groupGame.Winner?.Name ?? "nobody - it was a draw"}");
