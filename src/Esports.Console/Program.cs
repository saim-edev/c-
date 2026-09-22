// Values now go in through the constructor, not an initializer block.
// There is no way to build a Player without a tag and a rating.
Player faker = new Player("Faker", 1847);
Player chovy = new Player("Chovy", 1792);

Console.WriteLine("--- before the match ---");
Console.WriteLine($"{faker.GamerTag} - {faker.Rating}");
Console.WriteLine($"{chovy.GamerTag} - {chovy.Rating}");

// Faker beats Chovy. Neither line mentions the number 25 - the rule
// lives inside Player, so this code can't get it wrong.
faker.RecordWin();
chovy.RecordLoss();

Console.WriteLine();
Console.WriteLine("--- after the match ---");
Console.WriteLine($"{faker.GamerTag} - {faker.Rating}");
Console.WriteLine($"{chovy.GamerTag} - {chovy.Rating}");

// Reading is still fine - only WRITING from outside is blocked.
Console.WriteLine();
Console.WriteLine($"Is {faker.GamerTag} active? {faker.IsActive}");
