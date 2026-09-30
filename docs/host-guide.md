# Host Guide: Running a Draft Night

This guide walks you through running a Magic: The Gathering draft event using the MTG Draft Night app.

## Before the Event

### What You'll Need

- A device with a web browser (phone, tablet, or laptop)
- The app URL (provided by your organizer or self-hosted)
- A booster box (or know how many packs you have)
- 2-8 players

### Planning Your Prize Pool

The app calculates prize packs using this formula:

```
Prize Packs = Packs in Box - (3 × Number of Players)
```

| Players | Packs Used for Draft | Prize Packs (36-pack box) |
|---------|---------------------|---------------------------|
| 2 | 6 | 30 |
| 3 | 9 | 27 |
| 4 | 12 | 24 |
| 5 | 15 | 21 |
| 6 | 18 | 18 |
| 7 | 21 | 15 |
| 8 | 24 | 12 |

---

## Creating an Event

1. Open the app and tap **Host Event**
2. Enter your event details:
   - **Event Name** — Something descriptive like "Friday Night Draft"
   - **Packs in Box** — Total packs available (usually 36)
   - **Host PIN** — A password you'll use for host actions (remember this!)
3. Tap **Create Event**
4. You'll be taken to the lobby with a **join code** displayed

### Sharing the Join Code

Players can join in two ways:

1. **Share the link** — Tap "Copy Link" and send via text/Discord/etc.
2. **Share the code** — Tell players the 6-character code to enter manually

---

## The Lobby (Setup Phase)

While in the lobby:

- Players join and appear in the list with seed numbers
- Seed numbers are assigned in join order (first player = seed 1)
- You can **drop players** if someone joins by mistake
- The **Start Event** button enables when you have 2+ players

### Starting the Event

When everyone has joined:

1. Verify all players are listed
2. Tap **Start Event (N players)**
3. The app determines the format:
   - **2-4 players**: Round-robin (everyone plays everyone)
   - **5-8 players**: Swiss (3 rounds, paired by record)

---

## Running Rounds

### Pairings View

After starting, you'll see Round 1 pairings:

- Each match shows the two players
- **BYE** appears when a player has no opponent (odd number of players)
- Players can tap "Life Tracker" to open the integrated tracker

### Finalizing Matches

As matches complete:

1. Find the match in the pairings list
2. Tap the **winner's name** to select them
3. The match shows as "Final"

**Important**: Only the host can finalize matches. Players cannot report their own results.

### BYE Handling

- **Swiss format**: BYE counts as a win
- **Round-robin**: BYE (sit) does NOT count as a win

### Completing a Round

When all matches in a round are finalized:

1. The round status changes to "Round Closed"
2. If more rounds remain, tap **Start Round N** to publish next pairings
3. Players are paired based on current records (Swiss) or schedule (round-robin)

---

## Fixing Mistakes

### Reopening a Match

Made a mistake recording a winner?

1. Find the finalized match
2. Tap **Reopen**
3. Enter a reason (for the audit trail)
4. Select the correct winner

### Reopening an Entire Round

If multiple matches have errors, you can reopen the entire round at once:

1. Go to the **Admin** panel
2. Select **Reopen Round**
3. Choose the round to reopen
4. Enter a reason (for the audit trail)
5. All finalized non-BYE matches in that round will be reopened

**Note**: You can only reopen the current round (no subsequent rounds can exist).

### Swapping Opponents

Paired the wrong players together? You can swap players between matches without regenerating the entire round:

1. Go to the **Admin** panel
2. Select **Swap Opponents**
3. Choose the two matches and which player from each to swap
4. Enter a reason (for the audit trail)

**Requirements**:
- Both matches must be in the same round
- Neither match can be finalized
- Neither match can be a BYE
- Neither player can be dropped

### Regenerating Pairings

Need to completely redo the round's pairings?

1. Go to the **Admin** panel
2. Select **Regenerate Pairings**
3. Enter a reason (for the audit trail)
4. Confirm the action

**Requirements**:
- Must be the current round
- No non-BYE matches can be finalized

**Warning**: This creates entirely new pairings. Any match results in the round will need to be re-entered.

### Dropping a Player

If a player needs to leave mid-event:

1. Go to the **Lobby** tab (via navigation)
2. Find the player and tap the drop button
3. Enter a reason (optional)
4. Confirm the drop

Dropped players:
- Are removed from future pairings
- Keep their existing match results
- Cannot re-enter the pairings (they can still use **Rejoin** to follow the event)

---

## Standings

The **Standings** tab shows current rankings:

| Column | Meaning |
|--------|---------|
| Rank | Current position |
| Player | Name (highlighted if it's you) |
| Record | Wins - Losses |
| BYE | Shows if player received a bye |

Standings are sorted by:
1. Most wins
2. Lowest seed number (earlier joiners win ties)

---

## Prize Allocation

After the final round is complete:

1. Go to the **Prizes** tab
2. Review the prize pack count
3. Tap **Allocate Prizes**
4. Confirm the allocation

### How Prizes Are Distributed

Prizes go to players who won matches, prioritizing later rounds:

1. Players who won in Round 3 get packs first
2. Then Round 2 winners
3. Then Round 1 winners
4. Within a round, ties break by seed number

This rewards consistency — winning later rounds (against tougher opponents) matters more.

### Example (6 players, 18 prize packs)

| Player | Wins | Rounds Won | Packs |
|--------|------|------------|-------|
| Alice | 3-0 | R1, R2, R3 | 6 |
| Bob | 2-1 | R2, R3 | 5 |
| Carol | 2-1 | R1, R3 | 4 |
| Dave | 1-2 | R1 | 2 |
| Eve | 1-2 | R2 | 1 |
| Frank | 0-3 | — | 0 |

---

## Audit Log

The **Audit** tab (host only) shows a history of all actions:

- Event created/started
- Players joined/dropped
- Matches finalized/reopened
- Prizes allocated

Use this to:
- Verify what happened during disputes
- Review event history after the fact

---

## Life Tracker

### During Draft Matches

From the pairings view, tap **Life Tracker** on any match to open the integrated tracker:

- Starting life: 20
- +1/-1 and +5/-5 buttons
- Poison counters
- Game win toggles (best of 3)
- Top panel is inverted for face-to-face play

### Standalone Mode

The life tracker also works independently:

1. From the home screen, tap **Life Tracker**
2. Choose **Draft (1v1)** or **Commander**
3. Enter player names
4. Start tracking!

Commander mode supports:
- 2-6 players
- Starting life: 40
- Commander damage tracking
- Misc counters (energy, experience, etc.)

---

## Tips for Hosts

1. **Test beforehand** — Create a dummy event to familiarize yourself with the flow
2. **Have backup** — Keep paper and pen handy for emergencies
3. **Announce pairings** — Even with the app, verbally announce who plays who
4. **Set expectations** — Tell players only you can record results
5. **Check standings** — Verify standings look correct before allocating prizes

---

## Troubleshooting

### "Version conflict" error

Someone else made a change. Refresh and try again.

### Player can't join

- Verify they're using the correct code (case-insensitive)
- Check they haven't already joined with the same name
- Ensure the event hasn't started yet

### Phone died, or switched devices

Tap **Join Event**, then **Already joined on another device? Rejoin**. Enter the join code, the same name, and the PIN chosen when joining. For the host's seat, use the host PIN; this also restores host controls. The old device is signed out. After 10 failed attempts in a minute, wait a minute before trying again.

### App seems stuck

- Check your internet connection
- Refresh the page
- The app auto-reconnects after connection drops

### Wrong winner recorded

Use the **Reopen** function to correct it (see "Fixing Mistakes" above).

---

## Quick Reference

| Action | Where | Who |
|--------|-------|-----|
| Create event | Home → Host Event | Anyone |
| Join event | Home → Join Event | Anyone |
| Start event | Lobby | Host only |
| Finalize match | Pairings | Host only |
| Reopen match | Pairings | Host only |
| Reopen round | Admin | Host only |
| Swap opponents | Admin | Host only |
| Regenerate pairings | Admin | Host only |
| Drop player | Lobby | Host only |
| Allocate prizes | Prizes | Host only |
| View standings | Standings | Everyone |
| Use life tracker | Pairings or Home | Everyone |
