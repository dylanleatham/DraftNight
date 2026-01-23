import { useState, useCallback } from 'react'
import styles from './LifeDisplay.module.css'

export type AdjustmentButton = 1 | 5 | 10

interface LifeDisplayProps {
  life: number
  onLifeChange: (newLife: number) => void
  onAdjust: (delta: number) => void
  /** Which adjustment buttons to show (default: [1, 5]) */
  buttons?: AdjustmentButton[]
  /** Size variant */
  size?: 'normal' | 'compact'
  /** Whether the display is inverted (rotated 180deg) */
  inverted?: boolean
}

export function LifeDisplay({
  life,
  onLifeChange,
  onAdjust,
  buttons = [1, 5],
  size = 'normal',
  inverted = false,
}: LifeDisplayProps) {
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

  const containerClass = `${styles.container} ${styles[size]} ${inverted ? styles.inverted : ''}`

  return (
    <div className={containerClass}>
      <div className={styles.adjustButtons}>
        {buttons.map((amount) => (
          <button
            key={`minus-${amount}`}
            className={styles.adjustButton}
            onClick={() => onAdjust(-amount)}
            aria-label={`Subtract ${amount} life`}
          >
            -{amount}
          </button>
        ))}
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
        {buttons.map((amount) => (
          <button
            key={`plus-${amount}`}
            className={styles.adjustButton}
            onClick={() => onAdjust(amount)}
            aria-label={`Add ${amount} life`}
          >
            +{amount}
          </button>
        ))}
      </div>
    </div>
  )
}
