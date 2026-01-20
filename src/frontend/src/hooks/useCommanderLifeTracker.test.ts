import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useCommanderLifeTracker } from './useCommanderLifeTracker';
import { lifeTrackerStorage } from '../lib/lifeTrackerStorage';
import { COMMANDER_STARTING_LIFE, MAX_POISON } from '../types/lifeTracker';

// Mock the storage module
vi.mock('../lib/lifeTrackerStorage', () => ({
  lifeTrackerStorage: {
    getCommanderSession: vi.fn(),
    saveCommanderSession: vi.fn(),
  },
}));

describe('useCommanderLifeTracker', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    (lifeTrackerStorage.getCommanderSession as any).mockReturnValue(null);
  });

  it('initializes with default values for new session', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['Alice', 'Bob', 'Charlie', 'Diana'],
      })
    );

    expect(result.current.players).toHaveLength(4);
    expect(result.current.players[0].life).toBe(COMMANDER_STARTING_LIFE);
    expect(result.current.players[0].name).toBe('Alice');
    expect(result.current.players[0].poison).toBe(0);
    expect(result.current.players[0].commanderDamage).toHaveLength(3);
    expect(result.current.players[0].miscCounters).toHaveLength(0);
  });

  it('initializes commander damage tracking for all opponents', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3'],
      })
    );

    // Player 0 should have commander damage entries from players 1 and 2
    const player0 = result.current.players[0];
    expect(player0.commanderDamage).toHaveLength(2);
    expect(player0.commanderDamage.map((cd) => cd.fromPlayerId)).toContain('player_1');
    expect(player0.commanderDamage.map((cd) => cd.fromPlayerId)).toContain('player_2');
    expect(player0.commanderDamage.map((cd) => cd.fromPlayerId)).not.toContain('player_0');
  });

  it('adjusts life correctly', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    act(() => {
      result.current.adjustLife('player_0', -5);
    });

    expect(result.current.players[0].life).toBe(COMMANDER_STARTING_LIFE - 5);
  });

  it('sets life directly', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    act(() => {
      result.current.setLife('player_1', 25);
    });

    expect(result.current.players[1].life).toBe(25);
  });

  it('adjusts poison with clamping', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    act(() => {
      result.current.adjustPoison('player_0', 15);
    });

    expect(result.current.players[0].poison).toBe(MAX_POISON);

    act(() => {
      result.current.adjustPoison('player_0', -20);
    });

    expect(result.current.players[0].poison).toBe(0);
  });

  it('adjusts commander damage correctly', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    // Player 0 takes commander damage from player 1
    act(() => {
      result.current.adjustCommanderDamage('player_0', 'player_1', 5);
    });

    const player0 = result.current.players[0];
    const damageFromPlayer1 = player0.commanderDamage.find(
      (cd) => cd.fromPlayerId === 'player_1'
    );
    expect(damageFromPlayer1?.amount).toBe(5);
  });

  it('clamps commander damage to minimum 0', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    act(() => {
      result.current.adjustCommanderDamage('player_0', 'player_1', -10);
    });

    const player0 = result.current.players[0];
    const damageFromPlayer1 = player0.commanderDamage.find(
      (cd) => cd.fromPlayerId === 'player_1'
    );
    expect(damageFromPlayer1?.amount).toBe(0);
  });

  it('adds misc counter correctly', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    act(() => {
      result.current.addMiscCounter('player_0', 'Energy');
    });

    expect(result.current.players[0].miscCounters).toHaveLength(1);
    expect(result.current.players[0].miscCounters[0].name).toBe('Energy');
    expect(result.current.players[0].miscCounters[0].value).toBe(0);
  });

  it('removes misc counter correctly', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    act(() => {
      result.current.addMiscCounter('player_0', 'Energy');
    });

    const counterId = result.current.players[0].miscCounters[0].id;

    act(() => {
      result.current.removeMiscCounter('player_0', counterId);
    });

    expect(result.current.players[0].miscCounters).toHaveLength(0);
  });

  it('adjusts misc counter correctly', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    act(() => {
      result.current.addMiscCounter('player_0', 'Energy');
    });

    const counterId = result.current.players[0].miscCounters[0].id;

    act(() => {
      result.current.adjustMiscCounter('player_0', counterId, 5);
    });

    expect(result.current.players[0].miscCounters[0].value).toBe(5);

    act(() => {
      result.current.adjustMiscCounter('player_0', counterId, -2);
    });

    expect(result.current.players[0].miscCounters[0].value).toBe(3);
  });

  it('resets all players correctly', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    // Modify state
    act(() => {
      result.current.adjustLife('player_0', -10);
      result.current.adjustPoison('player_1', 5);
      result.current.adjustCommanderDamage('player_0', 'player_1', 10);
      result.current.addMiscCounter('player_2', 'Energy');
    });

    // Reset all
    act(() => {
      result.current.resetAll();
    });

    // Check all players are reset
    expect(result.current.players[0].life).toBe(COMMANDER_STARTING_LIFE);
    expect(result.current.players[1].poison).toBe(0);
    expect(result.current.players[0].commanderDamage.every((cd) => cd.amount === 0)).toBe(true);
    expect(result.current.players[2].miscCounters).toHaveLength(0);
  });

  it('saves to storage on state change', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    act(() => {
      result.current.adjustLife('player_0', -5);
    });

    expect(lifeTrackerStorage.saveCommanderSession).toHaveBeenCalled();
  });

  it('loads existing session from storage', () => {
    const existingSession = {
      id: 'test-session',
      mode: 'commander' as const,
      createdAt: Date.now(),
      updatedAt: Date.now(),
      startingLife: COMMANDER_STARTING_LIFE,
      players: [
        {
          id: 'player_0',
          name: 'Existing Player',
          life: 30,
          poison: 3,
          commanderDamage: [{ fromPlayerId: 'player_1', amount: 7 }],
          miscCounters: [{ id: 'test-counter', name: 'Treasure', value: 5 }],
        },
        {
          id: 'player_1',
          name: 'Player 2',
          life: 40,
          poison: 0,
          commanderDamage: [{ fromPlayerId: 'player_0', amount: 0 }],
          miscCounters: [],
        },
      ],
    };

    (lifeTrackerStorage.getCommanderSession as any).mockReturnValue(existingSession);

    const { result } = renderHook(() =>
      useCommanderLifeTracker({ sessionId: 'test-session' })
    );

    expect(result.current.players[0].name).toBe('Existing Player');
    expect(result.current.players[0].life).toBe(30);
    expect(result.current.players[0].poison).toBe(3);
    expect(result.current.players[0].miscCounters[0].value).toBe(5);
  });

  it('sets player name correctly', () => {
    const { result } = renderHook(() =>
      useCommanderLifeTracker({
        sessionId: 'test-session',
        playerNames: ['P1', 'P2', 'P3', 'P4'],
      })
    );

    act(() => {
      result.current.setPlayerName('player_0', 'New Name');
    });

    expect(result.current.players[0].name).toBe('New Name');
  });
});
