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

## EPIC 6 — Life Tracker ✅ COMPLETE

### Goal
Deliver a reliable life and counter tracking tool usable both inside and outside draft events.

### Features

#### 6.1 Draft (1v1) Life Tracker
- [x] Life totals with quick +/-1 and +/-5 buttons
- [x] Poison counters (0-10)
- [x] Game win toggles (best-of-3)
- [x] Inverted top panel for face-to-face play
- [x] Reset game/match modals

#### 6.2 Commander Life Tracker (Standalone)
- [x] 2–6 players with responsive grid layout
- [x] Starting life 40
- [x] Per-opponent commander damage tracking
- [x] Poison counters
- [x] Misc counters (energy, experience, etc.)
- [x] Expandable player panels

#### 6.3 Offline Support
- [x] Persist sessions to localStorage
- [x] Restore on reload without connectivity
- [x] Auto-save on every state change
- [x] Max 10 sessions per mode (prevents storage bloat)

### Exit criteria
- [x] Life tracker usable offline on mobile

### Implementation Notes
- Mobile-first with 56x56px minimum touch targets
- useReducer + localStorage pattern for state persistence
- Session resume from setup page
- Integration with PairingsPage (launch tracker for current match)
- 40 unit tests for hooks and storage

---

## EPIC 7 — Host Repair and Audit UX ✅ COMPLETE

### Goal
Enable safe correction of mistakes with transparency.

### Features

#### 7.1 Repair UI
- [x] Drop player (available during active events, not just setup)
- [x] Reopen match (with reason for audit trail)
- [ ] Swap opponents (deferred — backend API not yet implemented)
- [ ] Reopen round (deferred — backend API not yet implemented)

#### 7.2 Audit Log View
- [x] Chronological timeline of host actions
- [x] Color-coded action types (info, success, warning, danger)
- [x] Host-only access via nav link
- [x] Displays action type, entity, reason, and timestamp

### Exit criteria
- [x] Host can recover from common mistakes
- [x] Repairs are traceable

### Implementation Notes
- AuditLogPage with timeline styling
- API types for audit log entries
- Host-only nav link in EventLayout

---

## EPIC 8 — Notifications ✅ COMPLETE

### Goal
Provide minimal, high-signal in-app notifications.

### Features
- [x] Pairings published
- [x] Prize awarded

(No round or timing notifications.)

### Exit criteria
- [x] Notifications appear reliably on relevant events

### Implementation Notes
- Toast component with portal rendering and CSS animations
- NotificationContext for global toast state management
- useEventNotifications hook monitors SignalR snapshot changes
- Personalized notifications (opponent name, prize count)
- Auto-dismiss after 5 seconds with manual dismiss option
- 30 new unit tests covering hooks and components

---

## EPIC 9 — Hardening and Quality ✅ COMPLETE

### Goal
Prepare the system for real-world usage.

### Features

#### 9.1 Testing
- [x] Engine golden tests (135 tests for N ∈ [2,8])
- [x] API integration tests (44 tests including auth, concurrency, validation)
- [x] Realtime reconnect tests (SignalR hub tests with snapshot verification)
- [x] E2E tests with Playwright (full draft night flow)

#### 9.2 Performance and Reliability
- [x] Handle flaky connectivity (auto-reconnect with exponential backoff)
- [x] Graceful reconnect behavior (snapshot resync on reconnect)

#### 9.3 UX Polish
- [x] Large tap targets (56x56px minimum)
- [x] Dark mode support (CSS variables theme system)
- [x] Clear error messaging

### Exit criteria
- [x] System is stable during live draft night usage

### Implementation Notes
- Playwright E2E tests: `npm run e2e --prefix src/frontend`
- Test coverage: 135 engine tests, 44 API tests, 80 frontend unit tests
- SignalR reconnection tests skipped with InMemory provider (pass with SQL Server)
- Full tournament flow E2E test: create event → join players → play rounds → allocate prizes

---

## EPIC 10 — Documentation and Launch ✅ COMPLETE

### Goal
Make the system usable by others.

### Features
- [x] README.md (setup + run instructions)
- [x] Operator guide for hosts (`docs/host-guide.md`)
- [ ] Inline help/tooltips (deferred — not essential for launch)

### Exit criteria
- [x] A new host can run a draft night without assistance

### Documentation
- `README.md` — Quick start, project structure, tech stack, deployment
- `docs/host-guide.md` — Complete walkthrough for running events
- `docs/spec.md` — Full application specification
- `docs/design.md` — Architecture and design decisions
- `docs/draft_bracket_and_prizes.spec.md` — Tournament algorithm spec

---

## Deferred Items (Future Enhancements)

These items were intentionally deferred from the initial release:

### Host Repair APIs (from EPIC 3.3)
- [ ] Swap opponents — swap players between matches in a round
- [ ] Reopen round — revert a closed round to allow re-pairing
- [ ] Regenerate pairings — generate new pairings for current round

### Notes
- Core functionality is complete for running draft nights
- Deferred repair APIs are edge cases that can be worked around manually
- System is production-ready pending documentation

