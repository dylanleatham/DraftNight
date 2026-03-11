import styles from './LifeCounter.module.css'

interface LifeCounterProps {
  life: number
  onLifeChange: (newLife: number) => void
  onAdjust: (delta: number) => void
  color?: 'blue' | 'red' | 'green' | 'purple' | 'orange' | 'teal'
  inverted?: boolean
}

export function LifeCounter({
  life,
  onAdjust,
  color = 'blue',
  inverted = false,
}: LifeCounterProps) {
  return (
    <div
      className={`${styles.container} ${styles[color]} ${inverted ? styles.inverted : ''}`}
    >
      <div className={styles.adjustButtons}>
        <button
          className={styles.adjustButton}
          onClick={() => onAdjust(-5)}
          aria-label="Subtract 5 life"
        >
          -5
        </button>
        <button
          className={styles.adjustButton}
          onClick={() => onAdjust(-1)}
          aria-label="Subtract 1 life"
        >
          -1
        </button>
      </div>

      <div className={styles.lifeDisplay}>
        <span className={styles.lifeValue}>{life}</span>
      </div>

      <div className={styles.adjustButtons}>
        <button
          className={styles.adjustButton}
          onClick={() => onAdjust(1)}
          aria-label="Add 1 life"
        >
          +1
        </button>
        <button
          className={styles.adjustButton}
          onClick={() => onAdjust(5)}
          aria-label="Add 5 life"
        >
          +5
        </button>
      </div>
    </div>
  )
}
