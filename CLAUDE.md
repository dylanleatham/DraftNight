# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

MTG Draft Night Web Application — a mobile-first web app for running in-person Magic: The Gathering draft events (2–8 players). Manages player registration, match pairings, result tracking, prize allocation, and includes a standalone life tracker.

## Normative Specifications

**Critical**: The tournament engine MUST implement logic exactly as defined in `docs/draft_bracket_and_prizes.spec.md`. This includes:
- Round-robin (N ≤ 4) vs Swiss (N ≥ 5) format selection
- Circle-method scheduling for round-robin
- Swiss pairing with repeat-avoidance
- BYE handling (Swiss BYE = win; round-robin sit = no win)
- Later-round-first prize allocation when packs are insufficient

## Technology Stack

- **Backend**: .NET 10, ASP.NET Core Web API, Entity Framework Core, xUnit
- **Frontend**: React 19, TypeScript, Vite, Vitest, Playwright
- **API Contract**: OpenAPI document via Microsoft.AspNetCore.OpenApi (`/openapi/v1.json` in Development)
- **Hosting**: Single Docker container (API serves the built SPA from `wwwroot`) + SQL Server. No live deployment currently; originally ran on Azure.

## Repository Structure

```
src/
├── backend/
│   ├── DraftApp.Api/          # ASP.NET Core Web API
│   ├── DraftApp.Engine/       # Tournament engine (pure library)
│   └── DraftApp.Engine.Tests/ # Engine golden tests
└── frontend/                  # React SPA
docs/                          # Specifications and design documents
.github/workflows/             # CI/CD pipelines
```

## Common Commands

### Backend
```bash
# Run API locally
dotnet run --project src/backend/DraftApp.Api

# Run all backend tests
dotnet test src/backend

# Run engine tests only
dotnet test src/backend/DraftApp.Engine.Tests

# Apply database migrations
dotnet ef database update --project src/backend/DraftApp.Api

# Format code
dotnet format src/backend
```

### Frontend
```bash
# Install dependencies
npm install --prefix src/frontend

# Run dev server
npm run dev --prefix src/frontend

# Run tests
npm test --prefix src/frontend

# Build for production
npm run build --prefix src/frontend

# Lint and format
npm run lint --prefix src/frontend
```

### Local Environment
```bash
# Start local SQL Server only
docker compose up -d sqlserver

# Or run the whole app (SQL Server + API + SPA) at http://localhost:8080
docker compose up --build

# Populate a demo event
node scripts/seed-demo.mjs
```

## Key Domain Rules

- **Host-authoritative**: Only the host can finalize match results and perform repairs
- **No draws**: Every match produces exactly one winner
- **Deterministic tie-breaks**: MW desc → seed asc → id asc (applies to pairings, BYE assignment, prize allocation)
- **Prize formula**: `P = B - 3N` where B = box packs, N = players

## Tournament Engine Interface

Implement as pure functions operating on immutable state:
- `initializeEvent(players, packsInBox, seedOrder)`
- `generateRoundPairings(state, roundNumber)`
- `finalizeMatch(state, roundNumber, matchId, winnerId)`
- `dropPlayer(state, playerId)`
- `allocatePrizes(state)`

## Testing Policy

All new code changes must include corresponding unit tests. This is enforced to maintain correctness of the tournament engine and reliability of the application.

### Requirements

1. **Tournament Engine**: Every function must have unit tests covering:
   - All player counts N ∈ [2, 8]
   - Edge cases (odd N for BYEs, insufficient prize packs, repeat opponent scenarios)
   - Golden tests verifying determinism — identical inputs must produce identical outputs

2. **API Endpoints**: Integration tests for:
   - Authorization (host-only endpoints reject non-host requests)
   - Optimistic concurrency (version conflict returns 409)
   - Input validation (invalid winnerId, illegal state transitions)

3. **Realtime**: Tests for reconnection and state resync

### Test Location

Place tests adjacent to source files or in a parallel `__tests__` directory mirroring the source structure.

### What to Test

| Change Type | Required Tests |
|-------------|----------------|
| New engine function | Unit tests for all N values, edge cases, determinism |
| New API endpoint | Integration test for success, auth failure, validation |
| Bug fix | Regression test reproducing the bug |
| Refactor | Existing tests must pass; add tests if coverage gaps found |

### Golden Tests (Engine)

The tournament engine requires golden tests: fixed inputs (players, seeds, match results) with expected outputs committed to the repo. Any change that alters engine output must either:
- Fix a bug (update golden files with justification)
- Be rejected as a breaking change

## CI/CD

```
.github/workflows/
  ci.yml        # Build, format check, lint, test on pushes and PRs to main
```

- CI must pass before merging to main
- There is no deploy workflow; the `Dockerfile` produces a self-contained image
- The API applies EF Core migrations on startup when running against SQL Server

## Development Backlog

See `docs/backlog.md` for ordered epics. Critical path:
1. EPIC 0: Project foundations (repo, CI, tooling)
2. EPIC 1: Tournament engine (must pass golden tests for N ∈ [2,8])
3. EPIC 2-4: Persistence, API, realtime
4. EPIC 5-6: Frontend and life tracker
