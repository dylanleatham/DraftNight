import type { MiscCounter as MiscCounterType } from '../../types/lifeTracker'
import styles from './MiscCounter.module.css'

interface MiscCounterProps {
  counter: MiscCounterType
  onAdjust: (delta: number) => void
  onRemove: () => void
}

export function MiscCounter({ counter, onAdjust, onRemove }: MiscCounterProps) {
  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <span className={styles.name}>{counter.name}</span>
        <button
          className={styles.removeButton}
          onClick={onRemove}
          aria-label="Remove counter"
        >
          ×
        </button>
      </div>
      <div className={styles.controls}>
        <button className={styles.adjustButton} onClick={() => onAdjust(-1)}>
          -
        </button>
        <span className={styles.value}>{counter.value}</span>
        <button className={styles.adjustButton} onClick={() => onAdjust(1)}>
          +
        </button>
      </div>
    </div>
  )
}

interface MiscCounterListProps {
  counters: MiscCounterType[]
  onAdjust: (counterId: string, delta: number) => void
  onRemove: (counterId: string) => void
  onAdd: (name: string) => void
}

export function MiscCounterList({
  counters,
  onAdjust,
  onRemove,
  onAdd,
}: MiscCounterListProps) {
  const handleAddCounter = () => {
    const name = prompt('Counter name:')
    if (name?.trim()) {
      onAdd(name.trim())
    }
  }

  return (
    <div className={styles.listContainer}>
      {counters.map((counter) => (
        <MiscCounter
          key={counter.id}
          counter={counter}
          onAdjust={(delta) => onAdjust(counter.id, delta)}
          onRemove={() => onRemove(counter.id)}
        />
      ))}
      <button className={styles.addButton} onClick={handleAddCounter}>
        + Add Counter
      </button>
    </div>
  )
}
