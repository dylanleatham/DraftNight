import { MAX_POISON } from '../../types/lifeTracker'
import styles from './PoisonCounter.module.css'

interface PoisonCounterProps {
  poison: number
  onAdjust: (delta: number) => void
  inverted?: boolean
  compact?: boolean
}

export function PoisonCounter({
  poison,
  onAdjust,
  inverted = false,
  compact = false,
}: PoisonCounterProps) {
  const isLethal = poison >= MAX_POISON

  return (
    <div
      className={`${styles.container} ${inverted ? styles.inverted : ''} ${compact ? styles.compact : ''}`}
    >
      <button
        className={styles.adjustButton}
        onClick={() => onAdjust(-1)}
        disabled={poison <= 0}
        aria-label="Subtract poison"
      >
        -
      </button>
      <div
        className={`${styles.poisonDisplay} ${isLethal ? styles.lethal : ''}`}
      >
        <span className={styles.poisonIcon}>☠</span>
        <span className={styles.poisonValue}>{poison}</span>
      </div>
      <button
        className={styles.adjustButton}
        onClick={() => onAdjust(1)}
        disabled={poison >= MAX_POISON}
        aria-label="Add poison"
      >
        +
      </button>
    </div>
  )
}
