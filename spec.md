# MTG Draft Night Web Application — Specification

## 1. Purpose and Scope

This document defines the requirements for a web application that fully facilitates a Magic: The Gathering (MTG) draft night. The application manages player participation, match pairings, result tracking, prize eligibility, and provides integrated life‑total tracking utilities.

The application also exposes a standalone life‑tracking mode for non‑draft play, with a primary focus on Commander‑style games.

### In Scope
- Draft event creation and management
- Draft-night pairing/bracket execution
- Shared real-time state visible to all participants
- Match result tracking and prize eligibility notifications
- Integrated life and counter tracking for matches
- Standalone Commander life‑tracking utility

### Out of Scope (Initial Release)
- Booster draft pick tracking or card database integration
- Decklist construction or validation
- Rules adjudication or judge automation

---

## 2. User Roles

### Host (Organizer)
- Creates and configures draft events
- Starts rounds and publishes pairings
- Enters and finalizes match results
- Resolves disputes and performs repairs
- Controls prize allocation overrides

### Player (Participant)
- Joins draft events
- Views pairings and standings
- Uses life-tracking tools

The host role is authoritative for all state transitions.

---

## 3. Identity and Access Model

- Account creation is not required.
- Players join events using a link or join code.
- Player identity is defined by:
  - Display name
  - Edit PIN
- A single player identity may be active on **unlimited devices simultaneously**.

The PIN gates player‑scoped actions (e.g., name edits). All match results are finalized by the host only.

---

## 4. Core Domain Objects

### DraftEvent
- id
- name
- status: Setup | Active | Completed | Archived
- ruleset reference
- visibility: PublicLink | CodeRequired

### Player
- id
- displayName
- active: boolean
- dropped: boolean

### Round
- number
- status: Pending | PairingsPublished | Closed
- matches[]

### Match
- id
- roundNumber
- playerAId
- playerBId
- status: NotStarted | InProgress | Final
- winnerId
- loserId
- prizeEligible: boolean

### PrizeInventory
- totalPacksInBox
- packsUsedForDraft
- packsRemaining

---

## 5. Draft Event Lifecycle

### 5.1 Event Setup
- Host creates event and configures:
  - Player count
  - Box pack count (e.g., 30 or 36)
  - Ruleset reference

Validation:
- packsRemaining >= 0 must be satisfied before event start.

### 5.2 Player Join Phase
- Players join via link/code
- Lobby shows connected players
- Host starts the event

### 5.3 Rounds and Pairings
- Host starts each round explicitly
- Pairings are generated and published
- Pairings are visible to all participants in real time

### 5.4 Match Resolution
- Host enters and finalizes match results
- Match outcome determines prize eligibility

### 5.5 Event Completion
- Event is completed when host ends the final round
- Results become read‑only

---

## 6. Draft Pairings and Prize Allocation (Normative Reference)

The application **MUST** implement draft pairing logic, round progression, and prize allocation exactly as defined in the following normative specification:

- `draft_bracket_and_prizes.spec.md`

This includes, but is not limited to:
- Match assignment and round structure
- No‑draw enforcement
- BYE handling (if applicable)
- Prize eligibility rules
- Prize pack allocation prioritization

In the event of a conflict, the referenced specification takes precedence.

---

## 7. Shared State and Real‑Time Updates

The application maintains a shared event state observable by all participants.

Real‑time updates MUST include:
- Pairings being published
- Match results being finalized
- Prize packs being awarded

Consistency guarantees:
- All connected clients converge on the same event state
- Reconnection restores current authoritative state

---

## 8. Notifications

The application SHALL provide **in‑app notifications only** for the following events:
- Pairings published
- Prize awarded

The application SHALL NOT emit notifications for:
- Round timing
- Round ending
- Round lifecycle state changes

---

## 9. Life Tracker — Integrated and Standalone

### 9.1 Draft / 1v1 Mode
- Two players
- Configurable starting life (default 20)
- Life total adjustment controls
- Poison counters
- Optional game‑win tracking

When launched from an event match, the life tracker MAY assist the host in determining match outcome but does not finalize results automatically.

### 9.2 Commander Mode (Standalone)

Standalone mode MUST be usable without an active draft event.

Features:
- 2–6 players
- Starting life default 40
- Poison counters per player
- Commander damage tracking:
  - Per defending player
  - Per attacking commander
- Optional counters (e.g., energy, experience)
- Manual winner declaration

---

## 10. Offline Behavior

The **life tracker** MUST function offline:
- Life totals and counters remain usable without connectivity
- State is stored locally on the device

Offline behavior is limited strictly to life tracking. Match result submission and event state changes require connectivity.

---

## 11. Player Drops

- Players may be dropped **at any time** by the host.
- Dropped players are removed entirely from future pairings.
- If a player drops mid‑round:
  - Affected matches require host intervention per the normative pairing spec.

---

## 12. Host Repair and Administrative Tools

The host MUST have access to the following tools:
- Swap opponents within a round
- Force match results
- Reopen matches
- Reopen rounds
- Regenerate pairings
- Mark players as dropped or active

### Audit Logging
All host repair actions MUST be logged with:
- Timestamp
- Host identity
- Affected entity
- Before/after state
- Reason (free text)

Audit logs are immutable.

---

## 13. Reliability and Edge Cases

The system MUST correctly handle:
- Player disconnects and reconnections
- Duplicate device usage per player
- Host corrections after mistakes
- Event recovery after partial failure

The host is always the final authority on event state.

---

## 14. Non‑Functional Requirements

- Mobile‑first, responsive UI
- Usable in low‑connectivity environments
- Clear visual hierarchy for pairings and match status
- No hidden or automatic state transitions

---

## 15. Future Extensions (Non‑Binding)

The architecture SHOULD allow for future additions such as:
- Draft pick tracking
- Account‑based player profiles
- Historical event archives
- Advanced standings analytics

