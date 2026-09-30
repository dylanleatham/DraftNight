# DraftApp frontend

The React 19 + TypeScript + Vite SPA for MTG Draft Night. See the [root README](../../README.md) for the project overview and setup.

```bash
npm install
npm run dev        # http://localhost:5173 (proxies /api and /hubs to the API on :5244)
npm run test:run   # Vitest unit tests
npm run e2e        # Playwright; needs the API and dev server running
npm run lint && npm run format:check
```

Layout: `pages/` (routes), `components/` (`event/`, `life-tracker/`, `ui/`), `hooks/` (data and actions), `context/` (auth, event, notifications), `api/` (typed REST client), `lib/` (storage helpers).
