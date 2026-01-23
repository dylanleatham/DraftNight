import { renderHook, act } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useLifeTracker } from './useLifeTracker';
import { lifeTrackerStorage } from '../lib/lifeTrackerStorage';
import { DRAFT_STARTING_LIFE, MAX_POISON } from '../types/lifeTracker';

// Mock the storage module
vi.mock('../lib/lifeTrackerStorage', () => ({
  lifeTrackerStorage: {
    getDraftSession: vi.fn(),
    saveDraftSession: vi.fn(),
  },
}));

describe('useLifeTracker', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(lifeTrackerStorage.getDraftSession).mockReturnValue(null);
  });

  it('initializes with default values for new session', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    expect(result.current.playerA.life).toBe(DRAFT_STARTING_LIFE);
    expect(result.current.playerB.life).toBe(DRAFT_STARTING_LIFE);
    expect(result.current.playerA.poison).toBe(0);
    expect(result.current.playerB.poison).toBe(0);
    expect(result.current.gameWins.playerA).toBe(0);
    expect(result.current.gameWins.playerB).toBe(0);
  });

  it('uses provided player names', () => {
    const { result } = renderHook(() =>
      useLifeTracker({
        sessionId: 'test-session',
        playerAName: 'Alice',
        playerBName: 'Bob',
      })
    );

    expect(result.current.playerA.name).toBe('Alice');
    expect(result.current.playerB.name).toBe('Bob');
  });

  it('adjusts life correctly', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    act(() => {
      result.current.adjustLife('playerA', -5);
    });

    expect(result.current.playerA.life).toBe(DRAFT_STARTING_LIFE - 5);

    act(() => {
      result.current.adjustLife('playerB', 3);
    });

    expect(result.current.playerB.life).toBe(DRAFT_STARTING_LIFE + 3);
  });

  it('sets life directly', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    act(() => {
      result.current.setLife('playerA', 10);
    });

    expect(result.current.playerA.life).toBe(10);
  });

  it('adjusts poison correctly', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    act(() => {
      result.current.adjustPoison('playerA', 3);
    });

    expect(result.current.playerA.poison).toBe(3);
  });

  it('clamps poison to max value', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    act(() => {
      result.current.adjustPoison('playerA', 15);
    });

    expect(result.current.playerA.poison).toBe(MAX_POISON);
  });

  it('clamps poison to min value', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    act(() => {
      result.current.adjustPoison('playerA', -5);
    });

    expect(result.current.playerA.poison).toBe(0);
  });

  it('toggles game wins correctly', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    act(() => {
      result.current.toggleGameWin('playerA');
    });
    expect(result.current.gameWins.playerA).toBe(1);

    act(() => {
      result.current.toggleGameWin('playerA');
    });
    expect(result.current.gameWins.playerA).toBe(2);

    // Should wrap back to 0
    act(() => {
      result.current.toggleGameWin('playerA');
    });
    expect(result.current.gameWins.playerA).toBe(0);
  });

  it('resets game correctly', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    // Modify state
    act(() => {
      result.current.adjustLife('playerA', -10);
      result.current.adjustPoison('playerA', 5);
      result.current.toggleGameWin('playerA');
    });

    // Reset game
    act(() => {
      result.current.resetGame();
    });

    // Life and poison should reset
    expect(result.current.playerA.life).toBe(DRAFT_STARTING_LIFE);
    expect(result.current.playerA.poison).toBe(0);
    // But game wins should persist
    expect(result.current.gameWins.playerA).toBe(1);
  });

  it('resets match correctly', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    // Modify state
    act(() => {
      result.current.adjustLife('playerA', -10);
      result.current.adjustPoison('playerA', 5);
      result.current.toggleGameWin('playerA');
      result.current.toggleGameWin('playerA');
    });

    // Reset match
    act(() => {
      result.current.resetMatch();
    });

    // Everything should reset
    expect(result.current.playerA.life).toBe(DRAFT_STARTING_LIFE);
    expect(result.current.playerA.poison).toBe(0);
    expect(result.current.gameWins.playerA).toBe(0);
    expect(result.current.gameWins.playerB).toBe(0);
  });

  it('saves to storage on state change', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    act(() => {
      result.current.adjustLife('playerA', -5);
    });

    expect(lifeTrackerStorage.saveDraftSession).toHaveBeenCalled();
  });

  it('loads existing session from storage', () => {
    const existingSession = {
      id: 'test-session',
      mode: 'draft' as const,
      createdAt: Date.now(),
      updatedAt: Date.now(),
      startingLife: DRAFT_STARTING_LIFE,
      playerA: { id: 'playerA', name: 'Existing Player A', life: 15, poison: 2, miscCounters: [] },
      playerB: { id: 'playerB', name: 'Existing Player B', life: 18, poison: 0, miscCounters: [] },
      gameWins: { playerA: 1, playerB: 0 },
    };

    vi.mocked(lifeTrackerStorage.getDraftSession).mockReturnValue(existingSession);

    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    expect(result.current.playerA.name).toBe('Existing Player A');
    expect(result.current.playerA.life).toBe(15);
    expect(result.current.playerA.poison).toBe(2);
    expect(result.current.gameWins.playerA).toBe(1);
  });

  it('sets player name correctly', () => {
    const { result } = renderHook(() =>
      useLifeTracker({ sessionId: 'test-session' })
    );

    act(() => {
      result.current.setPlayerName('playerA', 'New Name');
    });

    expect(result.current.playerA.name).toBe('New Name');
  });
});
