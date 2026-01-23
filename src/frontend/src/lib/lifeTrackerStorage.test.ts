import { describe, it, expect, beforeEach, vi } from 'vitest';
import { lifeTrackerStorage } from './lifeTrackerStorage';
import type { DraftSession, CommanderSession } from '../types/lifeTracker';
import { DRAFT_STARTING_LIFE, COMMANDER_STARTING_LIFE } from '../types/lifeTracker';

// Mock localStorage
const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: vi.fn((key: string) => store[key] ?? null),
    setItem: vi.fn((key: string, value: string) => { store[key] = value; }),
    removeItem: vi.fn((key: string) => { delete store[key]; }),
    clear: vi.fn(() => { store = {}; }),
  };
})();

Object.defineProperty(window, 'localStorage', { value: localStorageMock });

function createDraftSession(id: string, overrides: Partial<DraftSession> = {}): DraftSession {
  const now = Date.now();
  return {
    id,
    mode: 'draft',
    createdAt: now,
    updatedAt: now,
    startingLife: DRAFT_STARTING_LIFE,
    playerA: { id: 'playerA', name: 'Player 1', life: DRAFT_STARTING_LIFE, poison: 0, miscCounters: [] },
    playerB: { id: 'playerB', name: 'Player 2', life: DRAFT_STARTING_LIFE, poison: 0, miscCounters: [] },
    gameWins: { playerA: 0, playerB: 0 },
    ...overrides,
  };
}

function createCommanderSession(id: string, playerCount: number = 4): CommanderSession {
  const now = Date.now();
  const playerIds = Array.from({ length: playerCount }, (_, i) => `player_${i}`);
  return {
    id,
    mode: 'commander',
    createdAt: now,
    updatedAt: now,
    startingLife: COMMANDER_STARTING_LIFE,
    players: playerIds.map((playerId, i) => ({
      id: playerId,
      name: `Player ${i + 1}`,
      life: COMMANDER_STARTING_LIFE,
      poison: 0,
      commanderDamage: playerIds.filter(p => p !== playerId).map(p => ({ fromPlayerId: p, amount: 0 })),
      miscCounters: [],
    })),
  };
}

describe('lifeTrackerStorage', () => {
  beforeEach(() => {
    localStorageMock.clear();
    vi.clearAllMocks();
  });

  describe('Draft Sessions', () => {
    it('returns empty array when no sessions exist', () => {
      const sessions = lifeTrackerStorage.getDraftSessions();
      expect(sessions).toEqual([]);
    });

    it('saves and retrieves a draft session', () => {
      const session = createDraftSession('test-session');
      lifeTrackerStorage.saveDraftSession(session);

      const retrieved = lifeTrackerStorage.getDraftSession('test-session');
      expect(retrieved).toBeTruthy();
      expect(retrieved?.id).toBe('test-session');
      expect(retrieved?.playerA.name).toBe('Player 1');
    });

    it('updates existing session', () => {
      const session = createDraftSession('test-session');
      lifeTrackerStorage.saveDraftSession(session);

      const updated = { ...session, playerA: { ...session.playerA, life: 15 } };
      lifeTrackerStorage.saveDraftSession(updated);

      const retrieved = lifeTrackerStorage.getDraftSession('test-session');
      expect(retrieved?.playerA.life).toBe(15);

      const sessions = lifeTrackerStorage.getDraftSessions();
      expect(sessions).toHaveLength(1);
    });

    it('deletes a session', () => {
      const session = createDraftSession('test-session');
      lifeTrackerStorage.saveDraftSession(session);

      lifeTrackerStorage.deleteDraftSession('test-session');

      const retrieved = lifeTrackerStorage.getDraftSession('test-session');
      expect(retrieved).toBeNull();
    });

    it('prunes old sessions when exceeding max count', () => {
      // Create 12 sessions (max is 10)
      for (let i = 0; i < 12; i++) {
        const session = createDraftSession(`session-${i}`, {
          updatedAt: Date.now() + i * 1000, // Each session is newer than the last
        });
        lifeTrackerStorage.saveDraftSession(session);
      }

      const sessions = lifeTrackerStorage.getDraftSessions();
      expect(sessions.length).toBeLessThanOrEqual(10);
    });
  });

  describe('Commander Sessions', () => {
    it('returns empty array when no sessions exist', () => {
      const sessions = lifeTrackerStorage.getCommanderSessions();
      expect(sessions).toEqual([]);
    });

    it('saves and retrieves a commander session', () => {
      const session = createCommanderSession('commander-test', 4);
      lifeTrackerStorage.saveCommanderSession(session);

      const retrieved = lifeTrackerStorage.getCommanderSession('commander-test');
      expect(retrieved).toBeTruthy();
      expect(retrieved?.id).toBe('commander-test');
      expect(retrieved?.players).toHaveLength(4);
    });

    it('updates existing session', () => {
      const session = createCommanderSession('commander-test', 4);
      lifeTrackerStorage.saveCommanderSession(session);

      const updated = {
        ...session,
        players: session.players.map((p, i) =>
          i === 0 ? { ...p, life: 35 } : p
        ),
      };
      lifeTrackerStorage.saveCommanderSession(updated);

      const retrieved = lifeTrackerStorage.getCommanderSession('commander-test');
      expect(retrieved?.players[0].life).toBe(35);
    });

    it('deletes a session', () => {
      const session = createCommanderSession('commander-test', 4);
      lifeTrackerStorage.saveCommanderSession(session);

      lifeTrackerStorage.deleteCommanderSession('commander-test');

      const retrieved = lifeTrackerStorage.getCommanderSession('commander-test');
      expect(retrieved).toBeNull();
    });
  });

  describe('getRecentSessions', () => {
    it('returns combined recent sessions sorted by updatedAt', () => {
      const draftSession = createDraftSession('draft-1', { updatedAt: 1000 });
      const commanderSession = { ...createCommanderSession('commander-1', 4), updatedAt: 2000 };

      lifeTrackerStorage.saveDraftSession(draftSession);
      lifeTrackerStorage.saveCommanderSession(commanderSession);

      const recent = lifeTrackerStorage.getRecentSessions();
      expect(recent.length).toBeGreaterThanOrEqual(2);
      // Most recent should be first
      expect(recent[0].updatedAt).toBeGreaterThanOrEqual(recent[1].updatedAt);
    });

    it('limits to 5 sessions', () => {
      for (let i = 0; i < 6; i++) {
        lifeTrackerStorage.saveDraftSession(createDraftSession(`draft-${i}`));
      }

      const recent = lifeTrackerStorage.getRecentSessions();
      expect(recent.length).toBeLessThanOrEqual(5);
    });
  });

  describe('clearAll', () => {
    it('clears all sessions', () => {
      lifeTrackerStorage.saveDraftSession(createDraftSession('draft-1'));
      lifeTrackerStorage.saveCommanderSession(createCommanderSession('commander-1', 4));

      lifeTrackerStorage.clearAll();

      expect(lifeTrackerStorage.getDraftSessions()).toEqual([]);
      expect(lifeTrackerStorage.getCommanderSessions()).toEqual([]);
    });
  });

  describe('error handling', () => {
    it('returns empty array for invalid JSON in localStorage', () => {
      localStorageMock.getItem.mockReturnValueOnce('invalid-json');
      const sessions = lifeTrackerStorage.getDraftSessions();
      expect(sessions).toEqual([]);
    });
  });
});
