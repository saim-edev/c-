// ---- build a team ----

Team t1 = new Team("T1", "T1");

t1.AddPlayer(new Player("Faker", 1847));
t1.AddPlayer(new Player("Gumayusi", 1791));
t1.AddPlayer(new Player("Keria", 1823));
t1.AddPlayer(new Player("Oner", 1768));
t1.AddPlayer(new Player("Zeus", 1802));

Console.WriteLine($"{t1.Name} has {t1.PlayerCount} players");
Console.WriteLine($"Average rating: {t1.AverageRating}");

// ---- the roster limit is enforced inside Team, not out here ----

Console.WriteLine();
Console.WriteLine("Trying to add a 6th player:");
t1.AddPlayer(new Player("Bench", 1500));
Console.WriteLine($"{t1.Name} still has {t1.PlayerCount} players");
