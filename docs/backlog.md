# backlog.md — MTG Draft Night Web Application

This backlog breaks the work into **epics → features → concrete tasks**, aligned with **spec.md**, **design.md**, and the normative algorithm spec (`draft_bracket_and_prizes.spec.md`).

The backlog is ordered roughly by **critical path**: items earlier enable later work.

---

## EPIC 0 — Project Foundations ✅ COMPLETE

### Goal
Establish the repository, development environment, and baseline architecture needed to build and test the system safely.

### Tasks
- [x] Initialize git repository
- [x] Define project structure (frontend, backend, engine)
- [x] Configure linting, formatting, and CI checks
- [x] Define environment configuration strategy (dev/prod)
- [x] Set up database schema migration tooling

**Exit criteria**
- [x] Repo builds and runs locally
- [x] CI passes on main branch

---

## EPIC 1 — Tournament Engine (Core Logic) ✅ COMPLETE

### Goal
Implement the deterministic pairing, standings, and prize logic exactly as defined in `draft_bracket_and_prizes.spec.md`.

This epic must be completed before most backend and UI work.

### Features

#### 1.1 Engine State Model
- Define engine-level Player, Match, Round, Event state
- Encode MW, ML, byeReceived, opponents, lastPlayedRound

#### 1.2 Event Initialization
- Initialize event with N players, seeds, and pack count B
- Compute derived values (format, roundsTotal, prizePacksAvailable)
- Validate `B - 3N >= 0`

#### 1.3 Round-Robin Pairing Algorithm
- Implement circle-method scheduling
- Handle odd-N sit BYEs (no win)
- Generate full match schedule deterministically

#### 1.4 Swiss Pairing Algorithm (3 Rounds)
- Round 1 adjacent-seed pairing
- Rounds 2–3 ranked pairing with repeat avoidance
- Swiss BYE assignment rules

#### 1.5 Match Result Application
- Apply winner updates (MW/ML)
- Track opponents and lastPlayedRound
- Enforce no-draw invariant

#### 1.6 Prize Allocation Algorithm
- Track Winners[r]
- Implement later-round-first prize distribution
- Implement within-round deterministic tie-break

#### 1.7 Validation and Error Handling
- Invalid winnerId
- Duplicate finalization
- Illegal state transitions

### Exit criteria
- [x] Pure engine passes golden tests for all N ∈ [2,8]
- [x] Engine output matches algorithm spec exactly

---

## EPIC 2 — Persistence and Data Model ✅ COMPLETE

### Goal
Persist canonical event facts and derived state safely.

### Features

#### 2.1 Database Schema
- Events
- Players
- Rounds
- Matches
- PrizeAllocations
- AuditLogs

#### 2.2 Event Versioning
- Add monotonic version field to events
- Enforce version checks on mutations

#### 2.3 Atomic Transactions
- Persist match results + derived state + audit log atomically

### Exit criteria
- [x] Event state can be reconstructed reliably from DB
- [x] Version conflicts are detected

---

## EPIC 3 — Backend API ✅ COMPLETE

### Goal
Expose host- and player-facing APIs that manipulate event state through the engine.

### Features

#### 3.1 Event Lifecycle APIs
- Create event
- Join event (name + PIN)
- Fetch event snapshot

#### 3.2 Host Control APIs
- Start event
- Publish pairings
- Finalize match result
- Drop player

#### 3.3 Host Repair APIs
- Swap opponents
- Reopen match
- Reopen round
- Regenerate pairings

#### 3.4 Authorization and Validation
- Enforce host-only endpoints
- Validate PINs securely (hashed)

### Exit criteria
- [x] All host flows callable via API
- [x] Unauthorized access is blocked

**Notes:**
- Host repair APIs partially implemented (ReopenMatch complete; SwapOpponents, ReopenRound, RegeneratePairings deferred)
- 2 integration tests skipped due to EF Core InMemory provider limitations (pass with SQL Server)

---

## EPIC 4 — Realtime Synchronization ✅ COMPLETE

### Goal
Ensure all participants observe the same authoritative state.

### Features

#### 4.1 Event Channels
- [x] One realtime channel per event (SignalR hub groups)

#### 4.2 Snapshot Broadcasting
- [x] Broadcast updated event snapshot on mutation

#### 4.3 Reconnect Handling
- [x] Client resyncs full snapshot on reconnect
- [x] Auto-reconnect with exponential backoff [0, 2s, 5s, 10s, 30s]
- [x] Visibility change triggers snapshot refresh

### Exit criteria
- [x] Two clients stay in sync during event progression

---

## EPIC 5 — Frontend: Core Event UX ✅ COMPLETE

### Goal
Provide a mobile-first UI for running a draft night.

### Features

#### 5.1 Join and Lobby
- [x] Join via link/code (with pre-filled code from URL)
- [x] Display joined players with seed numbers
- [x] Host start controls
- [x] Share join code with copy link button

#### 5.2 Pairings View
- [x] Show current round pairings
- [x] Highlight current player's match
- [x] Display match status (in progress, final)

#### 5.3 Match Finalization (Host)
- [x] Winner selection UI with large touch targets
- [x] Reopen match with reason (audit trail)
- [x] Publish next round button

#### 5.4 Standings View
- [x] Display MW–ML and BYE indicator
- [x] Deterministic ordering (MW desc, seed asc)
- [x] Highlight current player

#### 5.5 Prizes View
- [x] Remaining packs display
- [x] Per-player pack awards
- [x] Prize formula explanation
- [x] Confirmation modal for allocation

### Exit criteria
- [x] Full draft night can be run end-to-end via UI

### Implementation Notes
- React 18 + TypeScript + Vite
- CSS Modules for scoped styling
- React Router v7 for routing
- SignalR for real-time sync
- Mobile-first responsive design
- Dark/light theme support via CSS variables

---

## EPIC 6 — Life Tracker

### Goal
Deliver a reliable life and counter tracking tool usable both inside and outside draft events.

### Features

#### 6.1 Draft (1v1) Life Tracker
- Life totals
- Poison counters
- Game win toggles

#### 6.2 Commander Life Tracker (Standalone)
- 2–6 players
- Starting life 40
- Per-opponent commander damage
- Poison counters
- Optional misc counters

#### 6.3 Offline Support
- Persist sessions locally
- Restore on reload without connectivity

### Exit criteria
- Life tracker usable offline on mobile

---

## EPIC 7 — Host Repair and Audit UX

### Goal
Enable safe correction of mistakes with transparency.

### Features

#### 7.1 Repair UI
- Swap opponents
- Reopen match/round
- Drop player

#### 7.2 Audit Log View
- Chronological list of host actions
- Before/after diffs

### Exit criteria
- Host can recover from common mistakes
- Repairs are traceable

---

## EPIC 8 — Notifications

### Goal
Provide minimal, high-signal in-app notifications.

### Features
- Pairings published
- Prize awarded

(No round or timing notifications.)

### Exit criteria
- Notifications appear reliably on relevant events

---

## EPIC 9 — Hardening and Quality

### Goal
Prepare the system for real-world usage.

### Features

#### 9.1 Testing
- Engine golden tests
- API integration tests
- Realtime reconnect tests

#### 9.2 Performance and Reliability
- Handle flaky connectivity
- Graceful reconnect behavior

#### 9.3 UX Polish
- Large tap targets
- Dark mode support
- Clear error messaging

### Exit criteria
- System is stable during live draft night usage

---

## EPIC 10 — Documentation and Launch

### Goal
Make the system usable by others.

### Features
- README (setup + run)
- Operator guide for hosts
- Inline help/tooltips

### Exit criteria
- A new host can run a draft night without assistance

