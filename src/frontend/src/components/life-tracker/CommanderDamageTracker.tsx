import type { CommanderDamage } from '../../types/lifeTracker'
import { COMMANDER_DAMAGE_LETHAL } from '../../types/lifeTracker'
import styles from './CommanderDamageTracker.module.css'

interface CommanderDamageTrackerProps {
  commanderDamage: CommanderDamage[]
  playerNames: Map<string, string>
  onAdjust: (fromPlayerId: string, delta: number) => void
}

export function CommanderDamageTracker({
  commanderDamage,
  playerNames,
  onAdjust,
}: CommanderDamageTrackerProps) {
  return (
    <div className={styles.container}>
      <div className={styles.header}>Commander Damage</div>
      <div className={styles.damageList}>
        {commanderDamage.map((cd) => {
          const isLethal = cd.amount >= COMMANDER_DAMAGE_LETHAL
          const playerName = playerNames.get(cd.fromPlayerId) || 'Unknown'

          return (
            <div
              key={cd.fromPlayerId}
              className={`${styles.damageRow} ${isLethal ? styles.lethal : ''}`}
            >
              <span className={styles.playerName}>{playerName}</span>
              <div className={styles.controls}>
                <button
                  className={styles.adjustButton}
                  onClick={() => onAdjust(cd.fromPlayerId, -1)}
                  disabled={cd.amount <= 0}
                >
                  -
                </button>
                <span className={styles.amount}>{cd.amount}</span>
                <button
                  className={styles.adjustButton}
                  onClick={() => onAdjust(cd.fromPlayerId, 1)}
                >
                  +
                </button>
              </div>
            </div>
          )
        })}
      </div>
    </div>
  )
}
