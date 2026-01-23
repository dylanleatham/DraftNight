import type { RoundResponse, PlayerResponse } from '../../api/types';
import { MatchStatus, RoundStatus } from '../../api/types';
import styles from './BracketView.module.css';

interface BracketViewProps {
  rounds: RoundResponse[];
  players: PlayerResponse[];
  currentRound: number;
  currentPlayerId?: string | null;
}

export function BracketView({
  rounds,
  players,
  currentRound,
  currentPlayerId,
}: BracketViewProps) {
  const getPlayer = (id: string | null) =>
    id ? players.find((p) => p.id === id) : null;

  return (
    <div className={styles.container}>
      <div className={styles.bracket}>
        {rounds.map((round) => (
          <div
            key={round.roundNumber}
            className={`${styles.round} ${round.roundNumber === currentRound ? styles.currentRound : ''}`}
          >
            <div className={styles.roundHeader}>
              <span className={styles.roundTitle}>Round {round.roundNumber}</span>
              <span className={styles.roundStatus}>
                {round.status === RoundStatus.Pending && 'Upcoming'}
                {round.status === RoundStatus.PairingsPublished && 'In Progress'}
                {round.status === RoundStatus.Closed && 'Complete'}
              </span>
            </div>
            <div className={styles.matches}>
              {round.matches.map((match) => {
                const playerA = getPlayer(match.playerAId);
                const playerB = getPlayer(match.playerBId);
                const isUserMatch =
                  currentPlayerId === match.playerAId ||
                  currentPlayerId === match.playerBId;
                const isFinalized = match.status === MatchStatus.Final;

                return (
                  <div
                    key={match.id}
                    className={`${styles.match} ${isUserMatch ? styles.userMatch : ''} ${isFinalized ? styles.finalized : ''}`}
                  >
                    {match.isBye ? (
                      <div className={styles.byeMatch}>
                        <span className={styles.playerName}>
                          {playerA?.name || 'TBD'}
                        </span>
                        <span className={styles.byeLabel}>BYE</span>
                      </div>
                    ) : (
                      <>
                        <div
                          className={`${styles.player} ${match.winnerId === match.playerAId ? styles.winner : ''} ${isFinalized && match.winnerId !== match.playerAId ? styles.loser : ''}`}
                        >
                          <span className={styles.playerName}>
                            {playerA?.name || 'TBD'}
                          </span>
                          {match.winnerId === match.playerAId && (
                            <span className={styles.winIndicator}>W</span>
                          )}
                        </div>
                        <div className={styles.vs}>vs</div>
                        <div
                          className={`${styles.player} ${match.winnerId === match.playerBId ? styles.winner : ''} ${isFinalized && match.winnerId !== match.playerBId ? styles.loser : ''}`}
                        >
                          <span className={styles.playerName}>
                            {playerB?.name || 'TBD'}
                          </span>
                          {match.winnerId === match.playerBId && (
                            <span className={styles.winIndicator}>W</span>
                          )}
                        </div>
                      </>
                    )}
                    <span className={styles.matchCode}>{match.matchCode}</span>
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
