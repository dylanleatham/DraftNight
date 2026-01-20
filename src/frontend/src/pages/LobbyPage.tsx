import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Button, Modal, Input } from '../components/ui';
import { PlayerList } from '../components/event';
import { useEvent } from '../context/EventContext';
import { useAuth } from '../context/AuthContext';
import { useHostActions } from '../hooks/useHostActions';
import { EventStatus } from '../api/types';
import styles from './LobbyPage.module.css';

export function LobbyPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
  const { state } = useEvent();
  const { isHost, getPlayerId, hostSession } = useAuth();
  const { startEvent, dropPlayer } = useHostActions(eventId!);

  const [isStarting, setIsStarting] = useState(false);
  const [dropModalOpen, setDropModalOpen] = useState(false);
  const [playerToDrop, setPlayerToDrop] = useState<string | null>(null);
  const [dropReason, setDropReason] = useState('');

  const snapshot = state.snapshot;
  const isHostUser = isHost(eventId!);
  const playerId = getPlayerId(eventId!);

  if (!snapshot) return null;

  const isSetup = snapshot.status === EventStatus.Setup;
  const canStart = isSetup && snapshot.players.length >= 2;
  const activePlayers = snapshot.players.filter((p) => !p.isDropped);

  const handleStart = async () => {
    setIsStarting(true);
    const success = await startEvent();
    setIsStarting(false);
    if (success) {
      navigate(`/event/${eventId}/pairings`);
    }
  };

  const handleDropClick = (playerId: string) => {
    setPlayerToDrop(playerId);
    setDropReason('');
    setDropModalOpen(true);
  };

  const handleDropConfirm = async () => {
    if (!playerToDrop) return;
    await dropPlayer(playerToDrop, dropReason || undefined);
    setDropModalOpen(false);
    setPlayerToDrop(null);
    setDropReason('');
  };

  const playerToDropInfo = snapshot.players.find((p) => p.id === playerToDrop);

  return (
    <div className={styles.container}>
      {isSetup && (
        <div className={styles.joinInfo}>
          <p className={styles.joinLabel}>Share this code to invite players:</p>
          <div className={styles.joinCode}>{snapshot.joinCode}</div>
          {hostSession?.joinCode && (
            <button
              className={styles.copyButton}
              onClick={() => {
                navigator.clipboard.writeText(
                  `${window.location.origin}/join?code=${hostSession.joinCode}`
                );
              }}
            >
              Copy Link
            </button>
          )}
        </div>
      )}

      <div className={styles.section}>
        <div className={styles.sectionHeader}>
          <h2 className={styles.sectionTitle}>
            Players ({activePlayers.length})
          </h2>
          {isSetup && (
            <span className={styles.playerRange}>
              Min 2, Max 8
            </span>
          )}
        </div>

        <PlayerList
          players={snapshot.players}
          currentPlayerId={playerId}
          showStats={!isSetup}
          onDropPlayer={isHostUser ? handleDropClick : undefined}
        />
      </div>

      {isHostUser && isSetup && (
        <div className={styles.actions}>
          <Button
            size="large"
            fullWidth
            disabled={!canStart}
            loading={isStarting}
            onClick={handleStart}
          >
            Start Event ({activePlayers.length} players)
          </Button>
          {activePlayers.length < 2 && (
            <p className={styles.hint}>Need at least 2 players to start</p>
          )}
        </div>
      )}

      {!isSetup && (
        <div className={styles.eventInfo}>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Format</span>
            <span className={styles.infoValue}>
              {snapshot.format === 0 ? 'Round Robin' : 'Swiss'}
            </span>
          </div>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Rounds</span>
            <span className={styles.infoValue}>
              {snapshot.currentRound} / {snapshot.totalRounds}
            </span>
          </div>
          <div className={styles.infoItem}>
            <span className={styles.infoLabel}>Prize Packs</span>
            <span className={styles.infoValue}>{snapshot.prizePacks}</span>
          </div>
        </div>
      )}

      <Modal
        open={dropModalOpen}
        onClose={() => setDropModalOpen(false)}
        title="Drop Player"
      >
        <div className={styles.modalContent}>
          <p>
            Are you sure you want to drop{' '}
            <strong>{playerToDropInfo?.name}</strong>?
          </p>
          <Input
            label="Reason (optional)"
            value={dropReason}
            onChange={(e) => setDropReason(e.target.value)}
            placeholder="Enter reason..."
          />
          <div className={styles.modalActions}>
            <Button
              variant="secondary"
              onClick={() => setDropModalOpen(false)}
            >
              Cancel
            </Button>
            <Button variant="danger" onClick={handleDropConfirm}>
              Drop Player
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
}
