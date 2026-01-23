import type { PlayerResponse } from '../../api/types'
import { Badge } from '../ui'
import styles from './PlayerCard.module.css'

interface PlayerCardProps {
  player: PlayerResponse
  isCurrentUser?: boolean
  showStats?: boolean
  onDrop?: () => void
}

export function PlayerCard({
  player,
  isCurrentUser = false,
  showStats = false,
  onDrop,
}: PlayerCardProps) {
  return (
    <div
      className={`${styles.card} ${isCurrentUser ? styles.currentUser : ''} ${player.isDropped ? styles.dropped : ''}`}
    >
      <div className={styles.info}>
        <span className={styles.seed}>#{player.seed}</span>
        <span className={styles.name}>{player.name}</span>
        {isCurrentUser && <Badge variant="info">You</Badge>}
        {player.isDropped && <Badge variant="error">Dropped</Badge>}
        {player.byeReceived && <Badge variant="warning">BYE</Badge>}
      </div>
      {showStats && (
        <div className={styles.stats}>
          <span className={styles.record}>
            {player.matchWins}-{player.matchLosses}
          </span>
        </div>
      )}
      {onDrop && !player.isDropped && (
        <button
          className={styles.dropButton}
          onClick={onDrop}
          aria-label="Drop player"
        >
          Drop
        </button>
      )}
    </div>
  )
}
