// ============================================================================
//  THE ENTRY POINT
//
//  Unlike the console app's Program.cs, this one never reaches the bottom.
//  app.Run() at the end opens a port and WAITS, answering requests until
//  something kills the process.
//
//  The file has exactly two halves, split by builder.Build():
//
//    BEFORE Build()  -  a shopping list of things the app will need.
//                       Nothing is created yet.
//    AFTER Build()   -  what happens to every request that arrives.
// ============================================================================


// Reads configuration (appsettings.json, environment variables, command line)
// and gives us a builder to register things on.
var builder = WebApplication.CreateBuilder(args);


// ---- HALF 1: what this app will need -------------------------------------
//
// These lines do NOT create anything. They add entries to a list, which
// Build() below turns into real objects. See it as writing a shopping list,
// not doing the shopping.

// Lets the app use controller classes. Nothing uses them yet - the endpoint
// below is written the other way, directly in this file.
builder.Services.AddControllers();

// Publishes a machine-readable description of every endpoint at
// /openapi/v1.json, so tools can discover what this API offers.
builder.Services.AddOpenApi();


// The dividing line. Everything registered above is now built into a real,
// running application.
var app = builder.Build();


// ---- HALF 2: what happens to each request --------------------------------
//
// These run IN ORDER for every single request, before your code does.

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// If someone arrives on http, send them to https instead.
app.UseHttpsRedirection();

// Checks permissions. Nothing is protected yet, so this does nothing today.
app.UseAuthorization();

// "If a request matches a controller, send it there."
app.MapControllers();


// ---- THE ENDPOINTS -------------------------------------------------------

// The simplest possible endpoint.
//
//   "/health"  - the path someone asks for
//   () => ...  - the function to run when they do
//
// The object it returns is turned into JSON automatically. There is no
// step where anyone writes "{" or "}".
//
// A health endpoint is the first thing most APIs get: something that can be
// asked "are you alive?" without touching a database or needing a login.
app.MapGet("/health", () => new { status = "ok" });


// Opens the port and waits. This line does not finish while the app lives.
app.Run();
