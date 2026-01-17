# spec.md — MTG Draft Night Facilitator (2–8 Players)

## 1. Purpose

Build a web application that runs an in-person Magic: The Gathering draft night for **2–8 participants**, including:

- Player registration and seating (seed order)
- Draft pack accounting (box packs, draft consumption, prize pool)
- Match scheduling (round-robin for small N; Swiss for larger N)
- Result entry (best 2-of-3, **no draws**)
- Standings computation
- Prize allocation from remaining packs, with **priority to later-round winners** if packs are insufficient

This specification defines the **pairing algorithm**, **scoring/standings**, and **prize distribution** in a deterministic, implementable manner.

---

## 2. Definitions

- **N**: Number of players (integer, 2 ≤ N ≤ 8)
- **B**: Packs in booster box (integer; typically 30 or 36, but app should accept any integer ≥ 0)
- **Draft packs consumed**: `DraftConsumed = 3 * N`
- **Prize packs available**: `P = B - DraftConsumed`
- **Round formats**:
  - **Round-robin** for N ∈ {2, 3, 4}
  - **Swiss** for N ∈ {5, 6, 7, 8} with **R = 3 rounds**
- **Match**: Best 2-of-3, produces exactly one winner and one loser (no ties)
- **BYE**:
  - Round-robin: BYE means a player sits; **no win is awarded**
  - Swiss (odd N): BYE counts as a win; player receives **one match win** for that round

---

## 3. High-Level Flow

1. Organizer creates an Event
2. Organizer sets:
   - Box pack count B
   - Player list (names)
   - Optionally: randomize seed order (recommended) or manual ordering
3. App calculates:
   - Prize packs P
   - Format selection (RR vs Swiss)
   - Round count R
4. App generates Round 1 pairings
5. For each round:
   - Display pairings
   - Collect match winners
   - Update standings
   - Generate next round pairings (if applicable)
6. After final round:
   - Allocate prize packs based on results and prize policy
   - Display prize distribution summary

---

## 4. Core Data Model (Domain)

### 4.1 Player
- `id: string` (stable unique identifier)
- `name: string`
- `seed: number` (1..N; lower is “earlier”)
- `MW: number` (match wins; integer ≥ 0)
- `ML: number` (match losses; integer ≥ 0)
- `byeReceived: boolean`
- `opponents: string[]` (ordered list of opponent player ids; excludes BYE)
- `lastPlayedRound: Record<string, number>` (opponentId -> most recent round index)

### 4.2 Match (Pairing)
- `round: number` (1-indexed)
- `playerAId: string`
- `playerBId: string | null` (null means BYE match in Swiss)
- `result: { winnerId: string } | null` (null until entered)

### 4.3 Event
- `players: Player[]`
- `packsInBox: number` (B)
- `prizePacksAvailable: number` (P)
- `format: "ROUND_ROBIN" | "SWISS_3"`
- `roundsTotal: number` (R)
- `matchesByRound: Match[][]`
- `prizeAllocations: Record<string, number>` (playerId -> packs awarded)

---

## 5. Determinism Requirements

The app must generate consistent outcomes given the same inputs (players, seeds, results):

- Sorting ties MUST be resolved deterministically using:
  1) `MW` descending
  2) `seed` ascending
  3) `player.id` ascending (lexicographic)

This applies to:
- Swiss pairings
- BYE assignment
- Within-round prize allocation when packs are insufficient
- Final standings display

---

## 6. Format Selection and Round Counts

### 6.1 Selection
- If N ∈ {2,3,4}: format = ROUND_ROBIN
- If N ∈ {5,6,7,8}: format = SWISS_3

### 6.2 Round count R
- ROUND_ROBIN:
  - If N is even: R = N - 1
  - If N is odd: R = N (using a ghost BYE; player sits once)
- SWISS_3:
  - R = 3

---

## 7. Standings and Result Updates (No Draws)

When a match is completed (best 2-of-3):
- Winner: `MW += 1`
- Loser: `ML += 1`
- Both players append opponent to `opponents` list
- Update each player’s `lastPlayedRound[opponentId] = round`

### 7.1 Swiss BYE result
If Swiss and N is odd:
- BYE recipient in that round receives:
  - `MW += 1`
  - `byeReceived = true`
- BYE is not added to `opponents`

### 7.2 Round-robin “sit” BYE (odd N only)
- No MW/ML changes
- No opponents added

---

## 8. Pairing Algorithms

### 8.1 Round-robin schedule (N ∈ {2,3,4})
Use the **circle method**.

#### Inputs
- Players in seed order: array `A` of player IDs length N

#### Procedure
1. If N is odd, append ghost BYE id `0` to make `N_even = N + 1`.
2. Let `A` be an array length `N_even` (player IDs + optional 0).
3. Total rounds: `R = N_even - 1`
4. For each round `r = 1..R`:
   - Pair for each `i = 0..(N_even/2 - 1)`:
     - `x = A[i]`
     - `y = A[N_even - 1 - i]`
     - If `x == 0` or `y == 0`: the real player sits (no match, no win)
     - Else: create match (x vs y)
   - Rotate:
     - Keep A[0] fixed
     - New A = `[A[0], A[N_even-1], A[1], A[2], ..., A[N_even-2]]`

#### Output
A full deterministic match schedule with no repeats and one match per player per round (except sit rounds for odd N).

---

### 8.2 Swiss 3-round pairings (N ∈ {5,6,7,8})

#### 8.2.1 Round 1 pairings (adjacent seeds)
Let `S` be players sorted by seed ascending.

Pair in order:
- (S[0] vs S[1]), (S[2] vs S[3]), (S[4] vs S[5]), (S[6] vs S[7]) as available
If N is odd:
- Last unpaired player receives BYE (win)

#### 8.2.2 Rounds 2 and 3 pairings
At the start of each round r (r ∈ {2,3}):

##### Step A — Rank players
Create `ranked` list = players sorted by:
1) MW desc
2) seed asc
3) id asc

##### Step B — Assign BYE if N is odd
If N is odd:
- Select BYE recipient as the **lowest-ranked** player in `ranked` with `byeReceived == false`
- If all have `byeReceived == true`, select the absolute lowest-ranked player
- Create a BYE match record for that player (playerBId = null)
- Apply BYE win immediately for standings (MW++), mark paired for the round

##### Step C — Pair remaining players (repeat-avoidant Swiss)
Let `U` be the remaining unpaired players, ordered by the same ranking.

For each unpaired player `p` taken from top of `U`:
1. Candidates `C` = all unpaired players q != p
2. Prefer **non-repeat** opponents:
   - Let `C0 = { q ∈ C | q not in p.opponents }`
   - If `C0` non-empty, restrict to `C0`; else allow repeats (`C`)
3. Choose opponent q that minimizes this tuple (lexicographic):
   1) `recordGap = abs(MW[p] - MW[q])`  (prefer same record)
   2) `rankDistance = abs(index(p) - index(q))` within U
   3) If repeat is required, prefer the least-recent repeat:
      - `repeatAge = r - lastPlayedRound[p][q]` (larger is better)
      - Implement by sorting with `-repeatAge` so older repeats are chosen first
   4) `seed[q]` ascending
   5) `id[q]` ascending
4. Pair p vs q, mark both paired, create match record

##### Notes
- This greedy method is sufficient at N ≤ 8.
- If an implementation wants maximal repeat-avoidance, it may add limited backtracking. This is optional.

---

## 9. Prize Pack Accounting

### 9.1 Prize packs available
- `P = B - 3N`
- If `P < 0`: event cannot start; app must block progression and show error.

### 9.2 Prize eligibility rule
- Each **round win** makes the winner eligible for **1 prize pack**, subject to availability.
- Swiss BYE counts as a round win (eligible), because it increments MW.
- Round-robin sit BYE is not a win (not eligible).

### 9.3 Prize claims
Let `Winners[r]` = list of player IDs who won in round r (include BYE winners in Swiss).
Each player may appear multiple rounds (multiple wins) and is eligible for multiple packs.

---

## 10. Prize Allocation Algorithm (Later Rounds First)

### 10.1 Goal
If packs are insufficient to pay every round win, allocate prize packs prioritizing winners of later rounds.

### 10.2 Algorithm
Inputs:
- `P` prize packs available
- `R` total rounds
- `Winners[1..R]`

Procedure:
1. `packsRemaining = P`
2. Initialize `prizeAllocations[playerId] = 0` for all players
3. For rounds r from R down to 1:
   - If `packsRemaining == 0`: stop
   - Let `roundWinners = Winners[r]` (unique by match; BYE winner counts as a winner in Swiss)
   - If `roundWinners.length <= packsRemaining`:
     - Award 1 pack to each winner in `roundWinners`
     - `packsRemaining -= roundWinners.length`
   - Else (not enough packs for this round’s winners):
     - Select a subset of `packsRemaining` winners using the **within-round tie-break** (below)
     - Award 1 pack to each selected winner
     - Set `packsRemaining = 0` and stop

### 10.3 Within-round tie-break (deterministic)
When only some winners in a round can be paid, rank the round’s winners by:

1) **Standings after round r**:
   - higher MW first
2) seed ascending
3) id ascending

Award packs to the top K winners by this ranking, where K = packsRemaining.

### 10.4 Output
- Final `prizeAllocations` mapping (player -> packs)
- Remaining packs not allocated (if any) may be shown as “leftover” (organizer decision)

---

## 11. Validation and Guarantees

The system must guarantee:
- All matches have exactly one winner (except RR sit; except Swiss BYE)
- No player is paired twice in a round
- Swiss BYE is assigned fairly:
  - Prefer players who have not yet received a BYE
  - Otherwise lowest-ranked
- Prize allocation never exceeds available prize packs P
- Prize allocation prioritizes later rounds over earlier rounds

---

## 12. UI/UX Requirements (Minimum)

### 12.1 Event Setup
- Input: box pack count B
- Add players (2..8)
- Button: “Randomize seating / seeds” (optional but recommended)
- Display calculated:
  - DraftConsumed = 3N
  - PrizePacksAvailable P
  - Selected format and rounds
  - A warning if P is small relative to potential wins:
    - For Swiss: potential wins = `3 * ceil(N/2)`
    - For Round-robin: potential wins = `N*(N-1)/2`
  - If P < 0: block start

### 12.2 Round Screen
- Show pairings
- For each match: pick winner (A or B)
- For Swiss BYE match: auto-mark winner
- “Submit Round Results”:
  - updates standings
  - generates next round if any
  - stores Winners[r]

### 12.3 Standings Screen
- Table: Name, MW–ML, BYE received indicator
- Deterministic sorting rule applied

### 12.4 Prize Screen (after final round)
- Show total prize packs P
- Show per-player pack awards
- Show explanation:
  - “Later rounds are prioritized if packs are limited.”

---

## 13. Non-Goals / Explicit Exclusions

- Card pool management, deck registration, or game-level score tracking (only match winner)
- Tournament-level tiebreakers beyond MW/seed/id (OMW optional; not required)
- Multi-pod drafting support (this spec assumes a single pod of 2–8 players)
- Handling >8 players

---

## 14. Example Calculations (Reference)

### Swiss (N=8)
- DraftConsumed = 24
- If B=36 => P=12
- Winners per round = 4
- Potential wins = 12
- All round wins can be paid.

### Swiss (N=8, B=30)
- P=6, potential wins=12
- Prize allocation:
  - Round 3: pay 4 winners
  - Round 2: pay 2 winners (highest standings after R2)
  - Round 1: pay none

---

## 15. Acceptance Criteria

1. For every N in 2..8, the app selects the correct format and generates valid pairings.
2. With identical seeds and identical result inputs, the system produces identical pairings and prize allocations.
3. Prize packs distributed never exceed `B - 3N`, and later-round winners are prioritized when packs are insufficient.
4. Swiss odd-N BYE assignment follows fairness rule (no repeat BYEs until unavoidable), and BYE counts as a win.
5. Round-robin odd-N BYE results in a sit (no win).
6. The app provides a clear, complete record of:
   - all matches and winners
   - standings after each round
   - final prize allocation
