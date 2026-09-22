// The entry point of the React app. index.html loads exactly this one file.
//
// FLOW - what happens when the browser opens the page:
//   1. index.html is served and contains <div id="root"></div> plus a
//      <script type="module" src="/src/main.jsx">.
//   2. The browser runs this file.
//   3. createRoot(...) hands React ownership of that one <div>.
//   4. render(<App />) asks React to build the UI and put it inside the div.
// From that point on you never touch the DOM yourself - you change state,
// and React works out the minimum set of DOM edits needed.

import React from 'react'
// React 18 moved createRoot to the 'react-dom/client' entry point.
// (In React 17 and earlier this was ReactDOM.render from 'react-dom'.)
import { createRoot } from 'react-dom/client'
import App from './App.jsx'
import './index.css'

// StrictMode is a development-only wrapper. It ships NOTHING to production.
// It deliberately renders every component TWICE in dev to surface side effects
// that don't belong in a render. Expect to see console logs appear twice -
// that is the tool working, not a bug.
createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)
