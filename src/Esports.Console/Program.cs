// =====================================================================
//  1. A value type: int. The variable holds the NUMBER itself.
// =====================================================================

int x = 10;
int y = x;          // y gets a COPY of the number
y = y + 5;

Console.WriteLine("--- int (value type) ---");
Console.WriteLine($"x = {x}");      // 10 - untouched
Console.WriteLine($"y = {y}");      // 15


// =====================================================================
//  2. A reference type: Player. The variable holds the ADDRESS of an
//     object living elsewhere. Copying the variable copies the address.
// =====================================================================

Player a = new Player("Faker", 1847);
Player b = a;       // b gets a COPY OF THE ARROW, not a copy of the player
b.RecordWin();      // we only touch b...

Console.WriteLine();
Console.WriteLine("--- Player (reference type) ---");
Console.WriteLine($"a.Rating = {a.Rating}");   // ...but a changed too
Console.WriteLine($"b.Rating = {b.Rating}");

// ReferenceEquals asks the blunt question: are these the same object?
Console.WriteLine($"Same object? {ReferenceEquals(a, b)}");


// =====================================================================
//  3. Why this matters: the same player added to two teams.
// =====================================================================

Player shared = new Player("Faker", 1847);

Team t1 = new Team("T1", "T1");
Team gen = new Team("Gen.G", "GEN");

t1.AddPlayer(shared);
gen.AddPlayer(shared);      // NOT a second player - the same one, twice

shared.RecordWin();

Console.WriteLine();
Console.WriteLine("--- one player on two rosters ---");
Console.WriteLine($"T1    average: {t1.AverageRating}");
Console.WriteLine($"Gen.G average: {gen.AverageRating}");
Console.WriteLine("Both moved, because both rosters point at the SAME player.");
