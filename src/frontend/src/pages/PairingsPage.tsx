import { useState, useMemo } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Button, Modal, Input } from '../components/ui';
import { MatchCard, BracketView } from '../components/event';
import { useEvent } from '../context/EventContext';
import { useAuth } from '../context/AuthContext';
import { useHostActions } from '../hooks/useHostActions';
import { RoundStatus, MatchStatus } from '../api/types';
import styles from './PairingsPage.module.css';

type ViewMode = 'cards' | 'bracket';

export function PairingsPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
  const { state, getPlayer } = useEvent();
  const { isHost, getPlayerId } = useAuth();
  const { finalizeMatch, reopenMatch, publishPairings } = useHostActions(eventId!);

  const [reopenModalOpen, setReopenModalOpen] = useState(false);
  const [matchToReopen, setMatchToReopen] = useState<string | null>(null);
  const [reopenReason, setReopenReason] = useState('');
  const [isPublishing, setIsPublishing] = useState(false);
  const [viewMode, setViewMode] = useState<ViewMode>('cards');

  const snapshot = state.snapshot;
  const isHostUser = isHost(eventId!);
  const playerId = getPlayerId(eventId!);

  const currentRound = useMemo(() => {
    if (!snapshot) return null;
    return snapshot.rounds.find((r) => r.roundNumber === snapshot.currentRound);
  }, [snapshot]);

  if (!snapshot || !currentRound) return null;

  const isPairingsPublished = currentRound.status === RoundStatus.PairingsPublished;
  const isRoundClosed = currentRound.status === RoundStatus.Closed;
  const allMatchesFinalized = currentRound.matches.every(
    (m) => m.status === MatchStatus.Final
  );
  const canPublishNextRound =
    isHostUser &&
    isRoundClosed &&
    snapshot.currentRound < snapshot.totalRounds;

  const handleSelectWinner = async (matchId: string, winnerId: string) => {
    await finalizeMatch(matchId, winnerId);
  };

  const handleReopenClick = (matchId: string) => {
    setMatchToReopen(matchId);
    setReopenReason('');
    setReopenModalOpen(true);
  };

  const handleReopenConfirm = async () => {
    if (!matchToReopen || !reopenReason.trim()) return;
    await reopenMatch(matchToReopen, reopenReason.trim());
    setReopenModalOpen(false);
    setMatchToReopen(null);
    setReopenReason('');
  };

  const handlePublishNextRound = async () => {
    setIsPublishing(true);
    await publishPairings(snapshot.currentRound + 1);
    setIsPublishing(false);
  };

  const handleLaunchLifeTracker = (matchId: string, playerAId: string, playerBId: string | null) => {
    const playerAObj = getPlayer(playerAId);
    const playerBObj = playerBId ? getPlayer(playerBId) : undefined;
    const sessionId = `event_${eventId}_match_${matchId}`;
    const params = new URLSearchParams({
      mode: 'draft',
      playerA: playerAObj?.name || 'Player 1',
      playerB: playerBObj?.name || 'Player 2',
      eventId: eventId!,
      matchId,
      playerAId,
      ...(playerBId && { playerBId }),
    });
    navigate(`/life-tracker/game/${sessionId}?${params.toString()}`);
  };

  return (
    <div className={styles.container}>
      <div className={styles.roundHeader}>
        <div className={styles.roundInfo}>
          <h2 className={styles.roundTitle}>
            Round {snapshot.currentRound} of {snapshot.totalRounds}
          </h2>
          {isPairingsPublished && !allMatchesFinalized && (
            <span className={styles.status}>In Progress</span>
          )}
          {allMatchesFinalized && !isRoundClosed && (
            <span className={styles.statusComplete}>All Matches Complete</span>
          )}
          {isRoundClosed && (
            <span className={styles.statusClosed}>Round Closed</span>
          )}
        </div>
        <div className={styles.viewToggle}>
          <button
            className={`${styles.viewButton} ${viewMode === 'cards' ? styles.viewButtonActive : ''}`}
            onClick={() => setViewMode('cards')}
            aria-label="Card view"
            title="Card view"
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <rect x="1" y="1" width="6" height="6" rx="1" />
              <rect x="9" y="1" width="6" height="6" rx="1" />
              <rect x="1" y="9" width="6" height="6" rx="1" />
              <rect x="9" y="9" width="6" height="6" rx="1" />
            </svg>
          </button>
          <button
            className={`${styles.viewButton} ${viewMode === 'bracket' ? styles.viewButtonActive : ''}`}
            onClick={() => setViewMode('bracket')}
            aria-label="Bracket view"
            title="Bracket view"
          >
            <svg width="16" height="16" viewBox="0 0 16 16" fill="currentColor">
              <rect x="1" y="2" width="4" height="3" rx="0.5" />
              <rect x="1" y="11" width="4" height="3" rx="0.5" />
              <rect x="6" y="5.5" width="4" height="5" rx="0.5" />
              <rect x="11" y="6.5" width="4" height="3" rx="0.5" />
              <path d="M5 3.5h1.5v4h-1.5M5 12.5h1.5v-4h-1.5" stroke="currentColor" strokeWidth="1" fill="none" />
              <path d="M10 8h1" stroke="currentColor" strokeWidth="1" />
            </svg>
          </button>
        </div>
      </div>

      {viewMode === 'cards' ? (
        <div className={styles.matches}>
          {currentRound.matches.map((match) => (
            <MatchCard
              key={match.id}
              match={match}
              playerA={getPlayer(match.playerAId)}
              playerB={match.playerBId ? getPlayer(match.playerBId) : undefined}
              currentPlayerId={playerId}
              isHost={isHostUser}
              onSelectWinner={
                isPairingsPublished
                  ? (winnerId) => handleSelectWinner(match.id, winnerId)
                  : undefined
              }
              onReopen={
                isHostUser && match.status === MatchStatus.Final
                  ? () => handleReopenClick(match.id)
                  : undefined
              }
              onLifeTracker={
                isPairingsPublished && !match.isBye
                  ? () => handleLaunchLifeTracker(match.id, match.playerAId, match.playerBId)
                  : undefined
              }
            />
          ))}
        </div>
      ) : (
        <BracketView
          rounds={snapshot.rounds}
          players={snapshot.players}
          currentRound={snapshot.currentRound}
          currentPlayerId={playerId}
        />
      )}

      {canPublishNextRound && (
        <Button
          size="large"
          fullWidth
          loading={isPublishing}
          onClick={handlePublishNextRound}
        >
          Start Round {snapshot.currentRound + 1}
        </Button>
      )}

      {isHostUser && isRoundClosed && snapshot.currentRound === snapshot.totalRounds && (
        <div className={styles.tournamentComplete}>
          <p>Tournament complete! Go to Prizes to allocate prize packs.</p>
        </div>
      )}

      <Modal
        open={reopenModalOpen}
        onClose={() => setReopenModalOpen(false)}
        title="Reopen Match"
      >
        <div className={styles.modalContent}>
          <p>
            Enter a reason for reopening this match. This will be logged in the
            audit trail.
          </p>
          <Input
            label="Reason"
            value={reopenReason}
            onChange={(e) => setReopenReason(e.target.value)}
            placeholder="e.g., Incorrect winner selected"
            autoFocus
          />
          <div className={styles.modalActions}>
            <Button
              variant="secondary"
              onClick={() => setReopenModalOpen(false)}
            >
              Cancel
            </Button>
            <Button
              disabled={!reopenReason.trim()}
              onClick={handleReopenConfirm}
            >
              Reopen Match
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
