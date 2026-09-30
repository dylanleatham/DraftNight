import { useMemo } from 'react'
import type { PrizeAllocationResponse, PlayerResponse } from '../../api/types'
import { Badge } from '../ui'
import styles from './PrizeDisplay.module.css'

interface PrizeDisplayProps {
  prizeAllocations: PrizeAllocationResponse[]
  players: PlayerResponse[]
  currentPlayerId?: string | null
  totalPrizePacks: number
}

export function PrizeDisplay({
  prizeAllocations,
  players,
  currentPlayerId,
  totalPrizePacks,
}: PrizeDisplayProps) {
  const getPlayer = (playerId: string) => {
    return players.find((p) => p.id === playerId)
  }

  // Sort by packs awarded descending
  const sortedAllocations = useMemo(
    () => [...prizeAllocations].sort((a, b) => b.packsAwarded - a.packsAwarded),
    [prizeAllocations]
  )

  const allocatedPacks = prizeAllocations.reduce(
    (sum, a) => sum + a.packsAwarded,
    0
  )

  return (
    <div className={styles.container}>
      <div className={styles.summary}>
        <span className={styles.summaryLabel}>Prize Packs</span>
        <span className={styles.summaryValue}>
          {allocatedPacks} / {totalPrizePacks}
        </span>
      </div>

      <div className={styles.list}>
        {sortedAllocations.map((allocation) => {
          const player = getPlayer(allocation.playerId)
          const isCurrentUser = allocation.playerId === currentPlayerId

          return (
            <div
              key={allocation.playerId}
              className={`${styles.item} ${isCurrentUser ? styles.currentUser : ''}`}
            >
              <div className={styles.playerInfo}>
                <span className={styles.playerName}>
                  {player?.name ?? 'Unknown'}
                </span>
                {isCurrentUser && <Badge variant="info">You</Badge>}
              </div>
              <div className={styles.packsAwarded}>
                <span className={styles.packsCount}>
                  {allocation.packsAwarded}
                </span>
                <span className={styles.packsLabel}>
                  {allocation.packsAwarded === 1 ? 'pack' : 'packs'}
                </span>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
