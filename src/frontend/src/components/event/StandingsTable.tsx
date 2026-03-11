import type { StandingEntry } from '../../api/types'
import { Badge } from '../ui'
import styles from './StandingsTable.module.css'

interface StandingsTableProps {
  standings: StandingEntry[]
  currentPlayerId?: string | null
}

export function StandingsTable({
  standings,
  currentPlayerId,
}: StandingsTableProps) {
  return (
    <div className={styles.container}>
      <table className={styles.table} aria-label="Tournament Standings">
        <thead>
          <tr>
            <th className={styles.rankHeader}>#</th>
            <th className={styles.nameHeader}>Player</th>
            <th className={styles.recordHeader}>W-L</th>
          </tr>
        </thead>
        <tbody>
          {standings.map((entry) => (
            <tr
              key={entry.playerId}
              className={`${entry.playerId === currentPlayerId ? styles.currentUser : ''} ${entry.isDropped ? styles.dropped : ''}`}
            >
              <td className={styles.rank}>{entry.rank}</td>
              <td className={styles.name}>
                <span className={styles.playerName}>{entry.playerName}</span>
                {entry.playerId === currentPlayerId && (
                  <Badge variant="info">You</Badge>
                )}
                {entry.isDropped && <Badge variant="error">Dropped</Badge>}
              </td>
              <td className={styles.record}>
                {entry.matchWins}-{entry.matchLosses}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}
