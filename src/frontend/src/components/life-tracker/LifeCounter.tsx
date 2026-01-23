import { useState, useCallback } from 'react'
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
  onLifeChange,
  onAdjust,
  color = 'blue',
  inverted = false,
}: LifeCounterProps) {
  const [isEditing, setIsEditing] = useState(false)
  const [editValue, setEditValue] = useState('')

  const handleLifeClick = useCallback(() => {
    setEditValue(life.toString())
    setIsEditing(true)
  }, [life])

  const handleEditSubmit = useCallback(() => {
    const newLife = parseInt(editValue, 10)
    if (!isNaN(newLife)) {
      onLifeChange(newLife)
    }
    setIsEditing(false)
  }, [editValue, onLifeChange])

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'Enter') {
        handleEditSubmit()
      } else if (e.key === 'Escape') {
        setIsEditing(false)
      }
    },
    [handleEditSubmit]
  )

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

      <div className={styles.lifeDisplay} onClick={handleLifeClick}>
        {isEditing ? (
          <input
            type="number"
            className={styles.lifeInput}
            value={editValue}
            onChange={(e) => setEditValue(e.target.value)}
            onBlur={handleEditSubmit}
            onKeyDown={handleKeyDown}
            autoFocus
          />
        ) : (
          <span className={styles.lifeValue}>{life}</span>
        )}
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
