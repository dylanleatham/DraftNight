import { useEffect, useRef } from 'react';
import { useToast } from '../context/NotificationContext';
import { useAuth } from '../context/AuthContext';
import type { EventSnapshotResponse } from '../api/types';
import { RoundStatus } from '../api/types';

/**
 * Hook that monitors event state changes and shows notifications for:
 * - Pairings published (new round starts)
 * - Prizes awarded
 */
export function useEventNotifications(
  snapshot: EventSnapshotResponse | null,
  eventId: string
): void {
  const toast = useToast();
  const { getPlayerId } = useAuth();
  const prevSnapshotRef = useRef<EventSnapshotResponse | null>(null);

  const playerId = getPlayerId(eventId);

  useEffect(() => {
    if (!snapshot) return;

    const prevSnapshot = prevSnapshotRef.current;
    prevSnapshotRef.current = snapshot;

    // Skip notifications on initial load
    if (!prevSnapshot) return;

    // Detect pairings published
    detectPairingsPublished(prevSnapshot, snapshot, playerId, toast);

    // Detect prizes awarded
    detectPrizesAwarded(prevSnapshot, snapshot, playerId, toast);
  }, [snapshot, playerId, toast]);
}

function detectPairingsPublished(
  prev: EventSnapshotResponse,
  curr: EventSnapshotResponse,
  playerId: string | null,
  toast: ReturnType<typeof useToast>
): void {
  // Find rounds that just had pairings published
  for (const round of curr.rounds) {
    const prevRound = prev.rounds.find((r) => r.roundNumber === round.roundNumber);

    // Round existed before and status changed to PairingsPublished
    const justPublished =
      prevRound &&
      prevRound.status !== RoundStatus.PairingsPublished &&
      round.status === RoundStatus.PairingsPublished;

    // Or new round appeared with PairingsPublished status (first round being published)
    const newRoundPublished =
      !prevRound && round.status === RoundStatus.PairingsPublished;

    if (justPublished || newRoundPublished) {
      // Find the current player's match if they're in this round
      const playerMatch = playerId
        ? round.matches.find(
            (m) => m.playerAId === playerId || m.playerBId === playerId
          )
        : null;

      let opponentName: string | undefined;
      if (playerMatch) {
        const opponentId =
          playerMatch.playerAId === playerId
            ? playerMatch.playerBId
            : playerMatch.playerAId;
        if (opponentId) {
          const opponent = curr.players.find((p) => p.id === opponentId);
          opponentName = opponent?.name;
        } else if (playerMatch.isBye) {
          opponentName = undefined; // BYE
        }
      }

      if (playerMatch?.isBye) {
        toast.info(
          `Round ${round.roundNumber} Pairings`,
          `You have a BYE this round.`
        );
      } else if (opponentName) {
        toast.info(
          `Round ${round.roundNumber} Pairings`,
          `You're playing against ${opponentName}`
        );
      } else {
        toast.info(
          `Round ${round.roundNumber} Pairings`,
          'Pairings have been published!'
        );
      }
      break; // Only show one notification per update
    }
  }
}

function detectPrizesAwarded(
  prev: EventSnapshotResponse,
  curr: EventSnapshotResponse,
  playerId: string | null,
  toast: ReturnType<typeof useToast>
): void {
  // Check if prizes were just allocated
  if (!prev.prizesAllocated && curr.prizesAllocated) {
    // Find the current player's prize allocation
    const playerAllocation = playerId
      ? curr.prizeAllocations.find((p) => p.playerId === playerId)
      : null;

    if (playerAllocation && playerAllocation.packsAwarded > 0) {
      const packs = playerAllocation.packsAwarded;
      toast.success(
        'Prizes Awarded!',
        `You won ${packs} ${packs === 1 ? 'pack' : 'packs'}!`
      );
    } else if (playerAllocation) {
      toast.info('Prizes Awarded', 'Prize allocation is complete.');
    } else {
      toast.info('Prizes Awarded', 'Prize allocation is complete.');
    }
  }
}
