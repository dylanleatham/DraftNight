import type { MatchResponse, PlayerResponse } from '../../api/types';
import { MatchStatus } from '../../api/types';
import { Badge } from '../ui';
import styles from './MatchCard.module.css';

interface MatchCardProps {
  match: MatchResponse;
  playerA: PlayerResponse | undefined;
  playerB: PlayerResponse | undefined;
  currentPlayerId?: string | null;
  isHost: boolean;
  onSelectWinner?: (winnerId: string) => void;
  onReopen?: () => void;
}

export function MatchCard({
  match,
  playerA,
  playerB,
  currentPlayerId,
  isHost,
  onSelectWinner,
  onReopen,
}: MatchCardProps) {
  const isUserMatch =
    currentPlayerId === match.playerAId || currentPlayerId === match.playerBId;
  const canSelectWinner =
    isHost && match.status !== MatchStatus.Final && !match.isBye && onSelectWinner;
  const canReopen = isHost && match.status === MatchStatus.Final && onReopen;

  const getPlayerWinStatus = (playerId: string) => {
    if (match.status !== MatchStatus.Final) return null;
    return match.winnerId === playerId ? 'winner' : 'loser';
  };

  return (
    <div className={`${styles.card} ${isUserMatch ? styles.userMatch : ''}`}>
      <div className={styles.header}>
        <span className={styles.matchCode}>{match.matchCode}</span>
        {match.isBye && <Badge variant="warning">BYE</Badge>}
        {match.status === MatchStatus.Final && !match.isBye && (
          <Badge variant="success">Final</Badge>
        )}
      </div>

      {match.isBye ? (
        <div className={styles.byeContent}>
          <span className={styles.playerName}>{playerA?.name ?? 'Unknown'}</span>
          <span className={styles.byeText}>receives a bye</span>
        </div>
      ) : (
        <div className={styles.players}>
          <button
            className={`${styles.playerButton} ${getPlayerWinStatus(match.playerAId) === 'winner' ? styles.winner : ''} ${getPlayerWinStatus(match.playerAId) === 'loser' ? styles.loser : ''}`}
            disabled={!canSelectWinner}
            onClick={() => canSelectWinner && onSelectWinner(match.playerAId)}
          >
            <span className={styles.playerName}>{playerA?.name ?? 'Unknown'}</span>
            {getPlayerWinStatus(match.playerAId) === 'winner' && (
              <span className={styles.winnerIcon}>W</span>
            )}
          </button>

          <span className={styles.vs}>vs</span>

          <button
            className={`${styles.playerButton} ${getPlayerWinStatus(match.playerBId!) === 'winner' ? styles.winner : ''} ${getPlayerWinStatus(match.playerBId!) === 'loser' ? styles.loser : ''}`}
            disabled={!canSelectWinner}
            onClick={() =>
              canSelectWinner && match.playerBId && onSelectWinner(match.playerBId)
            }
          >
            <span className={styles.playerName}>{playerB?.name ?? 'Unknown'}</span>
            {getPlayerWinStatus(match.playerBId!) === 'winner' && (
              <span className={styles.winnerIcon}>W</span>
            )}
          </button>
        </div>
      )}

      {canReopen && (
        <button className={styles.reopenButton} onClick={onReopen}>
          Reopen Match
        </button>
      )}
    </div>
  );
}
