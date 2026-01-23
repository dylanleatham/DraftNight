import { renderHook } from '@testing-library/react';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { useEventNotifications } from './useEventNotifications';
import type { EventSnapshotResponse } from '../api/types';
import { EventStatus, RoundStatus, MatchStatus, TournamentFormat } from '../api/types';

// Mock dependencies
const mockToast = {
  success: vi.fn(),
  error: vi.fn(),
  warning: vi.fn(),
  info: vi.fn(),
  show: vi.fn(),
};

vi.mock('../context/NotificationContext', () => ({
  useToast: () => mockToast,
}));

vi.mock('../context/AuthContext', () => ({
  useAuth: () => ({
    getPlayerId: () => 'player-1',
  }),
}));

function createSnapshot(overrides: Partial<EventSnapshotResponse> = {}): EventSnapshotResponse {
  return {
    id: 'event-1',
    name: 'Test Event',
    status: EventStatus.Active,
    joinCode: 'ABC123',
    packsInBox: 36,
    prizePacks: 24,
    format: TournamentFormat.Swiss,
    totalRounds: 3,
    currentRound: 1,
    prizesAllocated: false,
    version: 1,
    players: [
      { id: 'player-1', name: 'Alice', seed: 1, matchWins: 0, matchLosses: 0, byeReceived: false, isDropped: false },
      { id: 'player-2', name: 'Bob', seed: 2, matchWins: 0, matchLosses: 0, byeReceived: false, isDropped: false },
      { id: 'player-3', name: 'Charlie', seed: 3, matchWins: 0, matchLosses: 0, byeReceived: false, isDropped: false },
      { id: 'player-4', name: 'Diana', seed: 4, matchWins: 0, matchLosses: 0, byeReceived: false, isDropped: false },
    ],
    rounds: [],
    prizeAllocations: [],
    ...overrides,
  };
}

describe('useEventNotifications', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('does not show notification on initial load', () => {
    const snapshot = createSnapshot();

    renderHook(() => useEventNotifications(snapshot, 'event-1'));

    expect(mockToast.info).not.toHaveBeenCalled();
    expect(mockToast.success).not.toHaveBeenCalled();
  });

  it('does not show notification when snapshot is null', () => {
    renderHook(() => useEventNotifications(null, 'event-1'));

    expect(mockToast.info).not.toHaveBeenCalled();
    expect(mockToast.success).not.toHaveBeenCalled();
  });

  describe('pairings published notifications', () => {
    it('shows notification when pairings are published', () => {
      const initialSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.Pending,
            matches: [],
          },
        ],
      });

      const updatedSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: false,
              },
              {
                id: 'match-2',
                matchCode: 'R1M2',
                playerAId: 'player-3',
                playerBId: 'player-4',
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: false,
              },
            ],
          },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      expect(mockToast.info).not.toHaveBeenCalled();

      rerender({ snapshot: updatedSnapshot });

      expect(mockToast.info).toHaveBeenCalledWith(
        'Round 1 Pairings',
        "You're playing against Bob"
      );
    });

    it('shows BYE notification when player has a BYE', () => {
      const initialSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.Pending,
            matches: [],
          },
        ],
      });

      const updatedSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: null,
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: true,
              },
              {
                id: 'match-2',
                matchCode: 'R1M2',
                playerAId: 'player-2',
                playerBId: 'player-3',
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: false,
              },
            ],
          },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      expect(mockToast.info).toHaveBeenCalledWith(
        'Round 1 Pairings',
        'You have a BYE this round.'
      );
    });

    it('shows notification when new round is created with pairings', () => {
      const initialSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.Closed,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: 'player-1',
                status: MatchStatus.Final,
                isBye: false,
              },
            ],
          },
        ],
      });

      const updatedSnapshot = createSnapshot({
        currentRound: 2,
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.Closed,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: 'player-1',
                status: MatchStatus.Final,
                isBye: false,
              },
            ],
          },
          {
            roundNumber: 2,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-3',
                matchCode: 'R2M1',
                playerAId: 'player-1',
                playerBId: 'player-3',
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: false,
              },
            ],
          },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      expect(mockToast.info).toHaveBeenCalledWith(
        'Round 2 Pairings',
        "You're playing against Charlie"
      );
    });
  });

  describe('prizes awarded notifications', () => {
    it('shows notification when prizes are allocated', () => {
      const initialSnapshot = createSnapshot({
        status: EventStatus.Active,
        prizesAllocated: false,
        prizeAllocations: [],
      });

      const updatedSnapshot = createSnapshot({
        status: EventStatus.Completed,
        prizesAllocated: true,
        prizeAllocations: [
          { playerId: 'player-1', packsAwarded: 5 },
          { playerId: 'player-2', packsAwarded: 3 },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      expect(mockToast.success).toHaveBeenCalledWith(
        'Prizes Awarded!',
        'You won 5 packs!'
      );
    });

    it('shows singular pack text when winning 1 pack', () => {
      const initialSnapshot = createSnapshot({
        prizesAllocated: false,
        prizeAllocations: [],
      });

      const updatedSnapshot = createSnapshot({
        prizesAllocated: true,
        prizeAllocations: [
          { playerId: 'player-1', packsAwarded: 1 },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      expect(mockToast.success).toHaveBeenCalledWith(
        'Prizes Awarded!',
        'You won 1 pack!'
      );
    });

    it('shows info notification when player won 0 packs', () => {
      const initialSnapshot = createSnapshot({
        prizesAllocated: false,
        prizeAllocations: [],
      });

      const updatedSnapshot = createSnapshot({
        prizesAllocated: true,
        prizeAllocations: [
          { playerId: 'player-1', packsAwarded: 0 },
          { playerId: 'player-2', packsAwarded: 5 },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      expect(mockToast.info).toHaveBeenCalledWith(
        'Prizes Awarded',
        'Prize allocation is complete.'
      );
    });
  });

  describe('match finalization notifications', () => {
    it('shows success notification when player wins a match', () => {
      const initialSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: false,
              },
            ],
          },
        ],
      });

      const updatedSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: 'player-1',
                status: MatchStatus.Final,
                isBye: false,
              },
            ],
          },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      expect(mockToast.success).toHaveBeenCalledWith(
        'Match Won!',
        'Victory against Bob!'
      );
    });

    it('shows info notification when player loses a match', () => {
      const initialSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: false,
              },
            ],
          },
        ],
      });

      const updatedSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: 'player-2',
                status: MatchStatus.Final,
                isBye: false,
              },
            ],
          },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      expect(mockToast.info).toHaveBeenCalledWith(
        'Match Complete',
        'Bob wins the match.'
      );
    });

    it('shows prize-eligible message for later round wins', () => {
      const initialSnapshot = createSnapshot({
        currentRound: 3,
        totalRounds: 3,
        prizePacks: 10,
        rounds: [
          {
            roundNumber: 3,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R3M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: false,
              },
            ],
          },
        ],
      });

      const updatedSnapshot = createSnapshot({
        currentRound: 3,
        totalRounds: 3,
        prizePacks: 10,
        rounds: [
          {
            roundNumber: 3,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R3M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: 'player-1',
                status: MatchStatus.Final,
                isBye: false,
              },
            ],
          },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      expect(mockToast.success).toHaveBeenCalledWith(
        'Match Won!',
        'Victory against Bob! This win is prize-eligible.'
      );
    });

    it('does not show notification for matches not involving the player', () => {
      const initialSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-3',
                playerBId: 'player-4',
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: false,
              },
            ],
          },
        ],
      });

      const updatedSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-3',
                playerBId: 'player-4',
                winnerId: 'player-3',
                status: MatchStatus.Final,
                isBye: false,
              },
            ],
          },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      // Should not show any notification for other players' matches
      expect(mockToast.success).not.toHaveBeenCalled();
      expect(mockToast.info).not.toHaveBeenCalled();
    });

    it('does not show notification for BYE matches', () => {
      const initialSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: null,
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: true,
              },
            ],
          },
        ],
      });

      const updatedSnapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: null,
                winnerId: 'player-1',
                status: MatchStatus.Final,
                isBye: true,
              },
            ],
          },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      // BYE matches should not trigger match finalization notification
      expect(mockToast.success).not.toHaveBeenCalled();
    });
  });

  describe('edge cases', () => {
    it('does not show notification if round status unchanged', () => {
      const snapshot = createSnapshot({
        rounds: [
          {
            roundNumber: 1,
            status: RoundStatus.PairingsPublished,
            matches: [
              {
                id: 'match-1',
                matchCode: 'R1M1',
                playerAId: 'player-1',
                playerBId: 'player-2',
                winnerId: null,
                status: MatchStatus.InProgress,
                isBye: false,
              },
            ],
          },
        ],
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot } }
      );

      // Same snapshot with different version
      const sameStatusSnapshot = {
        ...snapshot,
        version: 2,
      };

      rerender({ snapshot: sameStatusSnapshot });

      // Should not show notification since status didn't change
      expect(mockToast.info).not.toHaveBeenCalled();
    });

    it('does not show duplicate prize notifications', () => {
      const initialSnapshot = createSnapshot({
        prizesAllocated: true,
        prizeAllocations: [{ playerId: 'player-1', packsAwarded: 5 }],
      });

      const updatedSnapshot = createSnapshot({
        prizesAllocated: true,
        prizeAllocations: [{ playerId: 'player-1', packsAwarded: 5 }],
        version: 2,
      });

      const { rerender } = renderHook(
        ({ snapshot }) => useEventNotifications(snapshot, 'event-1'),
        { initialProps: { snapshot: initialSnapshot } }
      );

      rerender({ snapshot: updatedSnapshot });

      // No notification because prizesAllocated was already true
      expect(mockToast.success).not.toHaveBeenCalled();
    });
  });
});
