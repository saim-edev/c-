// A React component is just a function that returns a description of UI.
// It is NOT HTML - the JSX below compiles to React.createElement(...) calls.
// Braces {} mean "evaluate this JavaScript expression here".

export default function App() {
  return (
    <main className="shell">
      <h1>Esports Tournament Engine</h1>
      <p className="tagline">
        25-day C# / .NET backend curriculum &mdash; the React client lives here.
      </p>

      <section className="card">
        <h2>Status</h2>
        <ul>
          <li>React 18 + Vite client: <strong>scaffolded</strong></li>
          <li>ASP.NET Core Web API: <strong>scaffolded</strong></li>
          <li>Neon Postgres: <em>Day 10</em></li>
          <li>Wired to the API: <em>Day 17</em></li>
        </ul>
        <p className="note">
          This page is deliberately static. It does not call the API yet &mdash;
          doing that needs CORS, which is taught on Day 16 with the full
          browser preflight flow, and the client is wired up on Day 17.
        </p>
      </section>
    </main>
  )
}
