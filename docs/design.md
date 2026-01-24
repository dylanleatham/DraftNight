# design.md — MTG Draft Night Web Application

## 1. Design Goals

### Primary
- **Host-authoritative tournament control**: Host is the sole authority to finalize match outcomes and perform repairs.
- **Deterministic tournament engine**: Pairings, standings, and prizes are computed exactly per `draft_bracket_and_prizes.spec.md`.
- **Real-time shared state**: All participants see pairings, finalized results, and prize awards update promptly.
- **Mobile-first UX**: Optimized for phone usage at tables.
- **Resilience**: Clean reconnection behavior; offline life tracker.

### Secondary
- **Auditability**: Host repairs and overrides are logged immutably.
- **Extensibility**: Future capabilities (templates, exports, pick tracking) should be possible without re-architecting.

---

## 2. Technology Stack

### 2.1 Backend
- **.NET 10 (C#)** — ASP.NET Core Web API
- **Entity Framework Core** — ORM and migrations for Azure SQL
- **Azure SignalR SDK** — managed WebSocket integration
- **xUnit** — unit and integration testing

### 2.2 Frontend
- **React 18** with **TypeScript**
- **Vite** — build tooling
- **PWA** via vite-plugin-pwa (service worker, offline support)
- **Vitest** — unit testing
- **Playwright** — E2E testing

### 2.3 API Contract
- **OpenAPI 3.0** — generated from ASP.NET Core (Swashbuckle)
- **openapi-typescript-codegen** — generate TypeScript client from spec

### 2.4 Repository Structure (Monorepo)
```
/
├── src/
│   ├── backend/           # ASP.NET Core Web API
│   │   ├── DraftApp.Api/          # API project
│   │   ├── DraftApp.Engine/       # Tournament engine (pure library)
│   │   └── DraftApp.Engine.Tests/ # Engine golden tests
│   └── frontend/          # React SPA
│       ├── src/
│       └── public/
├── .github/
│   └── workflows/
├── docs/                  # Specs and design docs
└── docker-compose.yml     # Local dev environment
```

### 2.5 Local Development
- **Docker Compose** for local Azure SQL (via SQL Server container) and Azurite (storage emulator)
- Backend: `dotnet run` or VS Code / Visual Studio
- Frontend: `npm run dev` (Vite dev server with API proxy)

### 2.6 Code Quality
- **Backend**: `dotnet format`, StyleCop Analyzers
- **Frontend**: ESLint, Prettier
- **Pre-commit**: Husky + lint-staged (frontend), dotnet format check (backend)

---

## 3. High-Level Architecture

### 3.1 Components
- **Web Client (SPA/PWA)**
  - Event lobby, pairings, standings, prizes
  - Host control panel
  - Life tracker (Draft 1v1 and Commander)
  - Local persistence for life tracker sessions

- **Backend API (HTTP)**
  - Event CRUD, player join, host commands
  - Match result finalization
  - State queries (current event snapshot)

- **Realtime Gateway (WebSocket/SSE)**
  - Pushes authoritative updates to connected clients
  - Supports reconnect + resync

- **Tournament Engine (Pure module/service)**
  - Implements the algorithm in `draft_bracket_and_prizes.spec.md`
  - Deterministic functions operating on a tournament state model

- **Database (Relational recommended)**
  - Stores event state, rounds, matches, results, prizes
  - Stores audit log

### 3.2 Data Flow Summary
1. Client issues command (e.g., Host finalizes match)
2. API validates permissions and request shape
3. API applies mutation via Tournament Engine
4. Persist mutation + derived state atomically
5. Broadcast updated state/version via realtime channel
6. Clients update UI from push payload or fetch latest snapshot

---

## 3. State Model and Boundaries

### 3.1 Authoritative State
Authoritative tournament state lives on the backend. Clients are treated as **views** with limited command capabilities.

### 3.2 Client Local State
- Life tracker sessions are stored locally (device) to enable offline usage.
- Event-related state is cached for rendering but must be refreshed from backend on reconnect.

---

## 4. Domain Model (Logical)

This expands on spec.md’s domain objects for implementation clarity.

### 4.1 Entities

#### Event
- `eventId` (UUID)
- `name`
- `status`: `Setup | Active | Completed | Archived`
- `createdAt`, `updatedAt`
- `rules`:
  - `packsInBox (B)`
  - `format` (derived by engine)
  - `roundsTotal` (derived)
- `join`: `{ joinCode?, joinLinkToken }`
- `version` (monotonic integer for optimistic concurrency)

#### Player
- `playerId` (UUID)
- `eventId`
- `displayName`
- `pinHash` (salted hash)
- `seed` (integer 1..N)
- `active` boolean
- `dropped` boolean
- Engine fields (may be stored denormalized): `MW`, `ML`, `byeReceived`, etc.

#### Round
- `eventId`, `roundNumber`
- `status`: `Pending | PairingsPublished | Closed`
- `publishedAt`, `closedAt`

#### Match
- `matchId` (UUID)
- `eventId`, `roundNumber`
- `playerAId`
- `playerBId | null` (null indicates Swiss BYE per algorithm spec)
- `status`: `NotStarted | InProgress | Final`
- `winnerId`, `loserId` (nullable until final)
- `finalizedByHostId` (nullable)
- `finalizedAt` (nullable)

#### PrizeAllocation
- `eventId`
- `playerId`
- `packsAwarded` (integer)

#### AuditLog
- `auditId` (UUID)
- `eventId`
- `actorHostId`
- `actionType` (enum)
- `entityType` (Player/Match/Round/Event)
- `entityId`
- `beforeJson`, `afterJson`
- `reason`
- `createdAt`

### 4.2 Derived vs Stored
- **Source-of-truth inputs**: players (ids/names/seeds/dropped), packsInBox, match results (winner per match).
- **Derived state**: standings, next round pairings, prizes.

Recommended approach:
- Store match results as canonical facts.
- Compute derived state via engine on each relevant mutation and persist the resulting “event snapshot” fields (standings, prize allocations).

---

## 5. Tournament Engine Design

### 5.1 Interface (Recommended)
Implement a pure module with deterministic functions:

- `initializeEvent(players, packsInBox, seedOrder) -> EngineState`
- `generateRoundPairings(state, roundNumber) -> { state, pairings }`
- `finalizeMatch(state, roundNumber, matchId, winnerId) -> state`
- `dropPlayer(state, playerId) -> state`
- `repairSwapOpponents(state, roundNumber, matchAId, matchBId, ...) -> state`
- `reopenMatch(state, matchId) -> state`
- `reopenRound(state, roundNumber) -> state`
- `allocatePrizes(state) -> { state, allocations }`

Notes:
- The authoritative algorithm for Swiss/RR, BYEs, standings updates, and prizes is defined in `draft_bracket_and_prizes.spec.md`.
- All functions must be deterministic given identical inputs.

### 5.2 Engine State Shape
Align engine state closely to the algorithm spec:
- `players[]` (including MW/ML/opponents/byeReceived)
- `format`, `roundsTotal`
- `matchesByRound[][]`
- `winnersByRound[]` (for prize allocation)
- `prizeAllocations`

### 5.3 Validation Strategy
Engine returns structured validation errors for:
- illegal winnerId
- match already finalized
- dropping a player in a way that violates constraints (if any)
- attempting to finalize match when player dropped (policy-driven)

Backend converts these into user-friendly messages.

---

## 6. Backend API Design

### 6.1 API Principles
- Host-only state transitions that affect pairings/results/prizes.
- Idempotent commands where possible.
- Optimistic concurrency using `event.version`.

### 6.2 Suggested Endpoints (Illustrative)

#### Public/Player
- `POST /events/join` (join by code/token, name, pin)
- `GET /events/{eventId}` (snapshot)
- `GET /events/{eventId}/rounds/{r}` (pairings)

#### Host
- `POST /events` (create)
- `POST /events/{eventId}/start` (activate and publish round 1)
- `POST /events/{eventId}/rounds/{r}/publish` (publish pairings)
- `POST /events/{eventId}/matches/{matchId}/finalize` (winnerId)
- `POST /events/{eventId}/players/{playerId}/drop`
- `POST /events/{eventId}/repairs/swap-opponents`
- `POST /events/{eventId}/repairs/reopen-match`
- `POST /events/{eventId}/repairs/reopen-round`
- `POST /events/{eventId}/repairs/regenerate-pairings`

#### Read-only
- `GET /events/{eventId}/standings`
- `GET /events/{eventId}/prizes`
- `GET /events/{eventId}/audit`

Backend must enforce host authorization on host endpoints.

---

## 7. Realtime Design

### 7.1 Channel Model
- One realtime channel per event: `event:{eventId}`
- Clients subscribe after joining.

### 7.2 Message Types
- `EventSnapshotUpdated` (preferred): payload includes minimal diff or full snapshot + version
- `PairingsPublished` (optional if diffs are used)
- `MatchFinalized`
- `PrizeAwarded`

Given the small N (≤ 8), sending full snapshots is acceptable and simpler.

### 7.3 Reconnect and Resync
- Client tracks `event.version`.
- On reconnect:
  1) client requests latest snapshot
  2) server replies with authoritative snapshot and version
  3) client replaces local cached event state

---

## 8. Concurrency and Consistency

### 8.1 Optimistic Concurrency
- All mutating host commands require `expectedVersion`.
- If versions mismatch, backend returns `409 Conflict` with latest version and optionally latest snapshot.

### 8.2 Atomic Writes
- Persist:
  - canonical mutation (e.g., match winner)
  - recomputed derived state (standings/prizes)
  - audit log entry
  in a single transaction.

---

## 9. Security and Permissions

### 9.1 Roles
- Only Host can:
  - finalize match results
  - publish/regenerate pairings
  - drop players
  - reopen rounds/matches
  - override prize decisions

### 9.2 Authentication
- Event join: name + PIN
- Host authorization options:
  - host PIN created at event creation
  - or “host secret link token” stored client-side

Store PINs as salted hashes. Never store plaintext.

### 9.3 Abuse Controls
- Rate limit join attempts per IP/code.
- Prevent duplicate join spam by enforcing a max player count.

---

## 10. Offline Life Tracker Design

### 10.1 Storage
- Persist life tracker sessions in local storage (or IndexedDB):
  - session id
  - mode (Draft/Commander)
  - players and counters
  - timestamps

### 10.2 Sync Policy
- No automatic sync to backend.
- If opened from an event match, the tracker may display the match context but remains locally controlled.

---

## 11. Host Repair Tools Implementation

### 11.1 Philosophy
Repairs are explicit host commands that:
- create audit entries
- increment event version
- trigger recomputation of derived state

### 11.2 Repair Actions
- Swap opponents: modifies round pairings (and potentially match ids)
- Force result: sets match winner/loser
- Reopen match: clears winner/loser and adjusts standings
- Reopen round: marks round open and invalidates downstream rounds as needed
- Regenerate pairings: re-runs engine pairing for the round
- Drop player: removes from future pairing generation

Repairs may invalidate subsequent rounds; the UI must warn the host and show what downstream data is affected.

---

## 12. Observability

### 12.1 Logging
Include event-scoped structured logs:
- `eventId`, `roundNumber`, `matchId`, `actorHostId`

### 12.2 Metrics (Optional but recommended)
- events created/completed
- average time between pairing publish and all matches finalized
- repair frequency
- websocket disconnect rate

---

## 13. Deployment Notes (Azure)

### 13.1 Hosting Architecture
- **Frontend**: Azure Static Web Apps for SPA/PWA hosting (built-in HTTPS, global CDN)
- **Backend API**: Azure App Service (Linux) or Azure Container Apps
- **Realtime**: Azure SignalR Service (managed WebSocket scaling, handles connection limits and idle timeouts)
- **Database**: Azure SQL Database
- Single-region deployment is sufficient for in-person play.

### 13.2 Azure SQL Database
- Use Standard or Basic tier (S0 sufficient for expected load of ≤8 concurrent users per event)
- Enable automatic tuning
- Connection string stored in Azure App Service Configuration (not in code)
- Use managed identity for database authentication where possible

### 13.3 Azure SignalR Service
- Required because Azure App Service has a ~4 minute idle timeout on WebSocket connections
- **Use Default mode** (not Serverless) — ASP.NET Core SignalR hubs require persistent server connections which Serverless mode does not support
- Client reconnect logic (§7.3) remains necessary for network interruptions
- Hub per event not required; use groups for `event:{eventId}` channels

### 13.4 Rate Limiting
- Implement at application level using middleware (e.g., ASP.NET Core rate limiting)
- Alternatively, use Azure API Management for centralized rate limiting on join endpoints

### 13.5 Observability
- Azure Application Insights for:
  - Structured logging (eventId, roundNumber, matchId, actorHostId)
  - Metrics (events created/completed, WebSocket disconnect rate)
  - Dependency tracking (SQL queries, SignalR messages)
- Configure sampling to manage costs

### 13.6 Data Backup
- Azure SQL automatic backups enabled by default (7-day retention on Basic/Standard)
- Point-in-time restore available for active event recovery
- For long-term audit log retention, consider Azure Blob Storage export

### 13.7 CI/CD (GitHub Actions)

#### Workflows
- **CI (on push/PR to main)**: Build, lint, test (unit + integration)
- **Deploy (on push to main)**: Deploy to production after CI passes

#### Deployment Targets
| Component | Deployment Method |
|-----------|-------------------|
| Frontend (Static Web Apps) | Azure Static Web Apps GitHub Action (auto-configured on resource creation) |
| Backend (App Service) | `azure/webapps-deploy` action with publish profile or OIDC |
| Database migrations | Run as part of backend deployment pipeline |

#### Secrets and Configuration
- Use GitHub repository secrets for:
  - `AZURE_WEBAPP_PUBLISH_PROFILE` or OIDC credentials (`AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`)
  - `AZURE_SQL_CONNECTION_STRING` (for migrations in CI)
- Environment-specific configuration via Azure App Service Configuration (not in repo)

#### Workflow Structure
```
.github/
  workflows/
    ci.yml              # Build, lint, test on all PRs
    deploy.yml          # Deploy to Azure on push to main
```

#### Branch Protection
- Require CI to pass before merging to main
- Main branch deploys automatically to production

---

## 14. UI Composition (Implementation-Oriented)

### 14.1 Screens
- Join / Lobby
- Event Overview (pairings + quick nav)
- Round Pairings
- Match Detail (host finalize)
- Standings
- Prizes
- Host Admin (repairs + audit)
- Life Tracker: Draft
- Life Tracker: Commander

### 14.2 UI State Sources
- Event-related views: backend snapshot + realtime updates
- Life tracker: local storage

---

## 15. Testing Strategy (Design-Level)

- Pure engine: golden tests for determinism using fixed seeds/results.
- API: integration tests for version conflicts and authorization.
- Realtime: reconnection tests (drop socket, ensure resync).
- UI: critical paths on mobile viewport.

