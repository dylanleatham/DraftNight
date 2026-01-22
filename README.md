# MTG Draft Night

A mobile-first web app for running in-person Magic: The Gathering draft events (2–8 players). Manages player registration, match pairings, result tracking, prize allocation, and includes a standalone life tracker.

## Features

- **Event Management** — Create events, share join codes, manage players
- **Tournament Formats** — Round-robin (2-4 players) or Swiss (5-8 players)
- **Real-time Sync** — All participants see live updates via SignalR
- **Life Tracker** — Integrated 1v1 tracker for draft matches + standalone Commander mode
- **Prize Allocation** — Automatic prize calculation with later-round-first distribution
- **Host Controls** — Finalize matches, reopen for corrections, drop players
- **Audit Trail** — Full history of host actions for transparency
- **Offline Support** — Life tracker works without connectivity

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for local SQL Server)

### 1. Start the Database

```bash
docker-compose up -d
```

This starts SQL Server on port 1433.

### 2. Apply Migrations

```bash
dotnet ef database update --project src/backend/DraftApp.Api
```

### 3. Start the Backend

```bash
dotnet run --project src/backend/DraftApp.Api
```

The API runs at `http://localhost:5244`.

### 4. Start the Frontend

```bash
cd src/frontend
npm install
npm run dev
```

The app runs at `http://localhost:5173`.

### 5. Open the App

Navigate to http://localhost:5173 in your browser.

## Running Tests

### Backend Tests

```bash
# All backend tests
dotnet test src/backend

# Engine tests only (135 golden tests)
dotnet test src/backend/DraftApp.Engine.Tests

# API integration tests
dotnet test src/backend/DraftApp.Api.Tests
```

### Frontend Tests

```bash
# Unit tests (Vitest)
npm test --prefix src/frontend

# E2E tests (Playwright) — requires backend running
npm run e2e --prefix src/frontend
```

## Project Structure

```
src/
├── backend/
│   ├── DraftApp.Api/          # ASP.NET Core Web API
│   ├── DraftApp.Api.Tests/    # API integration tests
│   ├── DraftApp.Engine/       # Tournament engine (pure library)
│   └── DraftApp.Engine.Tests/ # Engine golden tests
└── frontend/                  # React SPA (Vite + TypeScript)
    └── e2e/                   # Playwright E2E tests

docs/
├── spec.md                    # Full application specification
├── design.md                  # Architecture and design decisions
├── draft_bracket_and_prizes.spec.md  # Tournament algorithm spec
└── backlog.md                 # Development backlog
```

## Technology Stack

| Layer | Technology |
|-------|------------|
| Frontend | React 18, TypeScript, Vite, CSS Modules |
| Backend | .NET 10, ASP.NET Core, Entity Framework Core |
| Real-time | SignalR |
| Database | SQL Server (Azure SQL in production) |
| Testing | xUnit, Vitest, Playwright |

## Configuration

### Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | SQL Server connection string | See appsettings |
| `AllowedOrigins` | CORS allowed origins | localhost:5173 |

### Local Development

The `docker-compose.yml` provides:
- **SQL Server** on port 1433 (sa/DraftApp_Dev123!)
- **Azurite** (Azure Storage emulator) on ports 10000-10002

## API Documentation

When running locally, Swagger UI is available at:
```
http://localhost:5244/swagger
```

## Key Concepts

### Tournament Formats

| Players | Format | Rounds |
|---------|--------|--------|
| 2-4 | Round-robin | N-1 rounds (everyone plays everyone) |
| 5-8 | Swiss | 3 rounds (paired by record) |

### Prize Formula

```
Prize Packs = Packs in Box - (3 × Number of Players)
```

Prizes are distributed later-round-first: players who won in later rounds get priority.

### Tie-breaking

All tie-breaks are deterministic:
1. Match wins (descending)
2. Seed number (ascending)
3. Player ID (ascending)

## Deployment

The app is designed for Azure:
- **Frontend**: Azure Static Web Apps
- **Backend**: Azure App Service
- **Database**: Azure SQL Database
- **Real-time**: Azure SignalR Service

See `.github/workflows/` for CI/CD pipelines.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Ensure all tests pass: `dotnet test src/backend && npm test --prefix src/frontend`
4. Submit a pull request

## License

MIT
