import { useMemo } from 'react'
import { useParams } from 'react-router-dom'
import { StandingsTable } from '../components/event'
import { useEvent } from '../context/EventContext'
import { useAuth } from '../context/AuthContext'
import type { StandingEntry } from '../api/types'
import styles from './StandingsPage.module.css'

export function StandingsPage() {
  const { eventId } = useParams<{ eventId: string }>()
  const { state } = useEvent()
  const { getPlayerId } = useAuth()

  const snapshot = state.snapshot
  const playerId = getPlayerId(eventId!)

  // Compute standings from snapshot players
  const standings = useMemo((): StandingEntry[] => {
    if (!snapshot) return []

    // Sort players by MW desc, then seed asc for tie-break
    const sorted = [...snapshot.players].sort((a, b) => {
      if (b.matchWins !== a.matchWins) {
        return b.matchWins - a.matchWins
      }
      return a.seed - b.seed
    })

    return sorted.map((player, index) => ({
      rank: index + 1,
      playerId: player.id,
      playerName: player.name,
      matchWins: player.matchWins,
      matchLosses: player.matchLosses,
      byeReceived: player.byeReceived,
      isDropped: player.isDropped,
    }))
  }, [snapshot])

  if (!snapshot) return null

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h2 className={styles.title}>Standings</h2>
        <span className={styles.roundInfo}>
          After Round {snapshot.currentRound}
        </span>
      </div>

      <StandingsTable standings={standings} currentPlayerId={playerId} />

      <div className={styles.tiebreakInfo}>
        <p>Tie-break: Match wins, then seed order</p>
      </div>
    </div>
  )
}
