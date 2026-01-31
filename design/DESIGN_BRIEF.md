# MTG Draft Night — Design Brief

## Project Overview

**App Name:** Draft Night  
**Domain:** draftnight.app  
**Purpose:** Manage in-person Magic: The Gathering draft events, track life totals, organize tournaments, and allocate prizes.

**Design Direction:** A deliberately grotesque, pixelated celebration of TCG culture's most infamous stereotypes. The visual language embraces the dungeon-dwelling, Cheeto-dusted, energy-drink-fueled reality of competitive card gaming.

---

## Design Philosophy

### The Vibe
> *"Welcome to the basement. The fluorescent lights flicker. Someone hasn't showered. The cards are sticky. Let's play some Magic."*

This isn't a clean, corporate esports aesthetic. This is the **LGS back room at 2 AM**. Crumbs on the playmat. Sweat beading on foreheads. Victory screams and defeated groans. We're leaning *hard* into the culture—with love, but without mercy.

### Pixel Art Direction
- **Resolution:** 256×256 base grid for detailed character work
- **Style:** Detailed pixel art reminiscent of late-90s PC RPGs and DOS games
- **Inspiration:** Darkest Dungeon, Papers Please, Kingdom of Loathing
- **Palette:** Dark, grimy, with neon accent pops (like a CRT monitor in a dark room)

### Tone
- **Self-aware humor:** We're laughing *with* the community, not at them
- **Grotesque affection:** Every greasy detail is rendered with artistic care
- **Retro nostalgia:** Pixel aesthetics evoke the era when many fell in love with MTG

---

## Character Archetypes

Each tournament participant is assigned a character archetype. These serve as visual identifiers throughout the app, appearing in pairings, life tracker, and winner celebrations. **Note:** There is no separate character selection screen—portraits are integrated directly into gameplay views.

### 1. The Card Shark 🦈
**File:** `Shark.png`
- **Traits:** Hooded, glowing eyes, menacing grin, impossible win streaks
- **Flavor:** *"666 wins, 0 losses. Coincidence? Probably not."*
- **Color Association:** Deep purple, gold accents
- **Use Case:** Default "unassigned" or "mysterious player" avatar

### 2. The Rules Lawyer 🧔
**File:** `RulesLawyer.png`
- **Traits:** Magnificent facial hair, food particles, skull t-shirt, soda cans
- **Flavor:** *"Actually, if you read the comprehensive rules..."*
- **Color Association:** Brown, orange (Cheeto dust), earth tones
- **Use Case:** Rules lawyer, experienced player

### 3. The Streamer 🎙️
**File:** `Streamer.png`
- **Traits:** Headset, multiple monitors, drooling, energy drinks, pointing at camera
- **Flavor:** *"WHAT'S UP CHAT let me show you this INSANE play"*
- **Color Association:** RGB rainbow, neon green, electric blue
- **Use Case:** Content creator, loud player

### 4. The Anime Fan 🍜
**File:** `Anime Fan.png`
- **Traits:** Anime figurines, Cup Noodles, manga stacks, sweat drops, trembling hands
- **Flavor:** *"My waifu commander will destroy you, b-baka!"*
- **Color Association:** Pink, pastel accents, anime aesthetic colors
- **Use Case:** Anime-sleeve enthusiast, weeb

### 5. The Strategist 📊
**File:** `Strategist.png`
- **Traits:** Papers everywhere, intense stare, coffee stains, tie (loosened), pointing dramatically
- **Flavor:** *"According to my spreadsheet, the optimal line is..."*
- **Color Association:** Muted greens, document cream, red accents
- **Use Case:** Spike player, min-maxer

### 6. The Final Boss 👑
**File:** `FinalBoss.png`
- **Traits:** Crown, slime-dripping tablet, triumphant pose, sitting on throne of cards
- **Flavor:** *"I've been waiting for a worthy opponent."*
- **Color Association:** Royal purple, gold, sickly green
- **Use Case:** Tournament winner, host, reigning champion

### 7. The Cosplayer ⚔️
**File:** `Cosplay.png`
- **Traits:** Viking helmet, armor (janky), convention banner, war cry
- **Flavor:** *"I AM the Gruul Clans!"*
- **Color Association:** Silver, red, medieval tones
- **Use Case:** Thematic player, Timmy/Tammy

### 8. The Wildcard 🃏
**File:** `Wildcard.png`
- **Traits:** Sunglasses, chaotic energy, cards flying, pet cat, piercing
- **Flavor:** *"Chaos draft? Planechase commander? I'm in."*
- **Color Association:** Warm orange, chaotic rainbow
- **Use Case:** Casual player, unpredictable

---

## Color System

### Primary Palette

| Token | Hex | Usage |
|-------|-----|-------|
| `--bg-void` | `#0a0a0c` | Deepest background, the void |
| `--bg-dungeon` | `#151519` | Primary background |
| `--bg-surface` | `#1f1f26` | Cards, panels, elevated surfaces |
| `--bg-elevated` | `#2a2a35` | Hover states, active elements |
| `--border-dim` | `#2d2d3a` | Subtle borders |
| `--border-glow` | `#4a4a5e` | Active borders |

### Text Hierarchy

| Token | Hex | Usage |
|-------|-----|-------|
| `--text-primary` | `#e8e8f0` | Primary text |
| `--text-secondary` | `#9090a8` | Secondary/muted text |
| `--text-dim` | `#5a5a70` | Disabled, placeholder |

### Accent Colors (The Neon Glow)

| Token | Hex | Usage |
|-------|-----|-------|
| `--accent-mana-blue` | `#4d9fff` | Primary actions, links |
| `--accent-mana-green` | `#3dd97a` | Success, wins, life gain |
| `--accent-mana-red` | `#ff5c5c` | Danger, drops, life loss |
| `--accent-mana-gold` | `#ffc53d` | Warnings, prizes, featured |
| `--accent-mana-purple` | `#a855f7` | Special, mythic, rare |

### Grime Accents (The Gross Stuff)

| Token | Hex | Usage |
|-------|-----|-------|
| `--grime-sweat` | `#7a8b4a` | Sweat drop indicators |
| `--grime-cheeto` | `#ff8c42` | Food stain effects |
| `--grime-slime` | `#4ade80` | Slimy/gross highlights |
| `--grime-stain` | `#8b6914` | Coffee/soda stains |

---

## Typography

### Font Stack

```css
--font-pixel: 'Press Start 2P', 'VT323', monospace;
--font-display: 'VT323', 'Courier New', monospace;
--font-body: 'IBM Plex Mono', 'Fira Code', monospace;
```

### Scale (8px base, pixel-perfect)

| Token | Size | Line Height | Usage |
|-------|------|-------------|-------|
| `--text-xs` | 8px | 12px | Micro labels, counters |
| `--text-sm` | 12px | 16px | Secondary info |
| `--text-base` | 16px | 24px | Body text |
| `--text-lg` | 20px | 28px | Subheadings |
| `--text-xl` | 24px | 32px | Section titles |
| `--text-2xl` | 32px | 40px | Page titles |
| `--text-3xl` | 48px | 56px | Life totals |
| `--text-huge` | 72px | 80px | Winner announcement |

---

## Spacing System (8px Grid)

All spacing follows an 8px base grid for pixel-perfect alignment.

| Token | Value |
|-------|-------|
| `--space-1` | 4px |
| `--space-2` | 8px |
| `--space-3` | 12px |
| `--space-4` | 16px |
| `--space-5` | 24px |
| `--space-6` | 32px |
| `--space-7` | 48px |
| `--space-8` | 64px |

---

## Component Specifications

### Buttons

**Primary Button**
- Background: `--accent-mana-blue`
- Text: `--bg-void`
- Border: 4px pixel border (darker shade)
- Hover: Brightness increase, pixel "press" effect (translate down 2px)
- Active: Invert colors briefly

**Danger Button**
- Same structure, `--accent-mana-red` palette

**Ghost Button**
- Transparent background
- Border: `--border-glow`
- Text: `--text-primary`

### Cards/Panels

```css
.panel {
  background: var(--bg-surface);
  border: 2px solid var(--border-dim);
  /* Pixel-perfect corners - no border-radius */
  box-shadow: 
    4px 4px 0 var(--bg-void),
    inset 0 0 20px rgba(0,0,0,0.5);
}
```

### Player Tiles (Life Tracker)

- Full-bleed color backgrounds per player
- Large, centered life total (72px+)
- Character avatar in corner (64×64px)
- Poison counter with skull icon
- Commander damage tracking
- "+/-" buttons with pixel styling

### Input Fields

```css
.input {
  background: var(--bg-void);
  border: 2px solid var(--border-dim);
  color: var(--text-primary);
  font-family: var(--font-body);
  /* Blinking cursor animation */
  caret-color: var(--accent-mana-blue);
}

.input:focus {
  border-color: var(--accent-mana-blue);
  box-shadow: 0 0 8px var(--accent-mana-blue);
}
```

---

## Screen Inventory

Based on the provided screenshots, these screens need to be designed:

### 1. Home Screen
- App title with pixel art logo
- "Create Event" button (primary)
- "Join Event" button (secondary)
- "Life Tracker" button (standalone utility)

### 2. Create Event
- Form fields: Event Name, Packs in Box, Host PIN
- "Create Event" button
- Back navigation

### 3. Join Event
- Form fields: Join Code, Your Name, Your PIN
- "Join Event" button
- Back navigation

### 4. Event Lobby (Host View)
- Event name + "Live" indicator
- Share code (large, copy button)
- Player list with character portraits (small, 48px)
- "Drop" button per player
- Tournament settings display
- "Start Event" button (disabled until min players)

### 5. Pairings View
- Round indicator ("Round 1 of 3")
- Status badge (IN PROGRESS / ROUND CLOSED)
- **Match cards with character portraits prominently displayed (64px)**
- Click on player to select winner
- "Life Tracker" quick-launch per match
- Winner indicator (green highlight + trophy emoji)
- "Reopen Match" for hosts

### 6. Standings View
- Leaderboard table
- Rank, Player (with character portrait), W-L record
- Tie-break explanation

### 7. Prizes View
- Prize pool display
- Per-player allocation with character portraits
- "Allocate Prizes" button
- Formula explanation

### 8. Audit Log
- Chronological event list
- Entry types: PLAYER JOINED, EVENT CREATED, MATCH RESULT, etc.
- Timestamps

### 9. Life Tracker Setup
- Mode toggle: Draft (1v1) / Commander
- Player count selector (Commander: 2/3/4)
- Player name inputs
- Recent sessions list
- "Start Game" button

### 10. Life Tracker — Draft Mode
- Two player panels (top/bottom split)
- **Character portrait in corner of each panel (48px)**
- Life totals (20 starting)
- +1/+5/-1/-5 buttons
- Poison counter
- **Game win tracker with character portraits on either side**
- Reset options

### 11. Life Tracker — Commander Mode
- 2-4 player panels (grid layout)
- **Character portrait in corner of each panel (48px)**
- Life totals (40 starting)
- Commander damage tracking per opponent
- Custom counter support
- Panel customization (color/image)

### 12. Match Winner Modal
- **Winner's character portrait (large, 96px) with archetype name**
- Winner player name
- "Reset" / "Done" buttons
- Confetti/celebration effect

### 13. Customize Panel Modal
- Color picker (preset grid + hex input)
- Image upload option
- Apply/cancel buttons

---

## Character Portrait Integration

Rather than a separate character selection screen, character portraits are integrated directly into gameplay views:

### Portrait Sizes
- **Small (48px):** Player lists, corners of life tracker panels, game win tracker
- **Medium (64px):** Match cards in pairings view
- **Large (96px):** Winner modal celebration

### Portrait Placement
- **Lobby:** Small portrait next to each player name in the list
- **Pairings:** Medium portrait centered above player name in match cards
- **Life Tracker Panels:** Small portrait in top-right corner (rotates with panel)
- **Game Win Tracker:** Small portraits flanking the score (e.g., 🦈 2 — 1 🧔)
- **Winner Modal:** Large portrait with archetype name displayed below

### Assignment
Characters are assigned to players when they join an event. The assignment can be:
- Automatic (round-robin through available archetypes)
- Player-selected during join flow
- Host-assigned from lobby

---

## Animation & Effects

### Pixel Transitions
- No smooth easing — use `steps()` for all animations
- Example: `transition: transform 0.1s steps(2);`

### CRT Effects (Optional, Toggle-able)
```css
.crt-effect {
  animation: flicker 0.15s infinite;
  box-shadow: inset 0 0 60px rgba(0, 255, 0, 0.1);
}

.scanlines::after {
  background: repeating-linear-gradient(
    0deg,
    rgba(0, 0, 0, 0.15),
    rgba(0, 0, 0, 0.15) 1px,
    transparent 1px,
    transparent 2px
  );
}
```

### Life Change Animation
- Flash green on gain, red on loss
- Number "ticks" up/down in steps
- Screen shake on death (0 life)

### Winner Celebration
- Confetti particles (pixel squares)
- Flashing border
- Character avatar zoom

---

## Sound Design Notes (For Future Implementation)

- **Button press:** Chunky 8-bit click
- **Life change:** Blip up/down
- **Match win:** Triumphant chiptune jingle
- **Death:** Sad descending tone
- **Error:** Harsh buzz

---

## Accessibility Considerations

Despite the grimy aesthetic, ensure:
- Color contrast meets WCAG AA minimum
- Touch targets minimum 44×44px
- Screen reader labels on all interactive elements
- Reduce motion option (disables CRT/animations)
- High contrast mode alternative

---

## File Deliverables

1. `tokens.css` — All CSS custom properties
2. `preview.html` — Static design system preview
3. `DraftNightDemo.jsx` — Interactive React component demo
4. Character avatar sprites (already provided)

---

## Implementation Notes

### Tech Stack Assumptions
- React/Next.js frontend
- Tailwind CSS (with custom pixel theme)
- CSS custom properties for theming
- LocalStorage for session persistence

### Asset Requirements
- Optimize PNGs for web (current ~300KB each, target ~100KB)
- Generate 64×64 thumbnail versions for UI use
- Consider WebP format for browser support

---

*"May your draws be favorable and your opponents' sleeves be regulation.*"

— The Draft Night Design Team
