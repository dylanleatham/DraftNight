import { useState, useCallback, useRef, useEffect } from 'react'
import styles from './LifeDisplay.module.css'

function DeltaToast({ delta, visible }: { delta: number; visible: boolean }) {
  if (delta === 0 && !visible) return null

  const sign = delta >= 0 ? '+' : ''
  return (
    <div
      className={`${styles.deltaToast} ${visible ? styles.deltaToastVisible : ''}`}
    >
      {sign}
      {delta}
    </div>
  )
}

export type AdjustmentButton = 1 | 5 | 10

interface LifeDisplayProps {
  life: number
  onLifeChange: (newLife: number) => void
  onAdjust: (delta: number) => void
  /** Which adjustment buttons to show (default: [1, 5]) */
  buttons?: AdjustmentButton[]
  /** Size variant: normal (large 2-player), medium (3-4 player), compact (minimal) */
  size?: 'normal' | 'medium' | 'compact'
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
  const [cumulativeDelta, setCumulativeDelta] = useState(0)
  const [toastVisible, setToastVisible] = useState(false)
  const hideTimeoutRef = useRef<number | null>(null)
  const resetTimeoutRef = useRef<number | null>(null)

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

  const handleAdjust = useCallback(
    (delta: number) => {
      // Clear existing timeouts
      if (hideTimeoutRef.current) window.clearTimeout(hideTimeoutRef.current)
      if (resetTimeoutRef.current) window.clearTimeout(resetTimeoutRef.current)

      // Accumulate delta and show toast
      setCumulativeDelta((prev) => prev + delta)
      setToastVisible(true)

      // Hide after 1 second
      hideTimeoutRef.current = window.setTimeout(() => {
        setToastVisible(false)
        // Reset delta after fade animation completes
        resetTimeoutRef.current = window.setTimeout(() => {
          setCumulativeDelta(0)
        }, 200)
      }, 1000)

      // Call parent handler
      onAdjust(delta)
    },
    [onAdjust]
  )

  // Cleanup timeouts on unmount
  useEffect(() => {
    return () => {
      if (hideTimeoutRef.current) window.clearTimeout(hideTimeoutRef.current)
      if (resetTimeoutRef.current) window.clearTimeout(resetTimeoutRef.current)
    }
  }, [])

  const containerClass = `${styles.container} ${styles[size]} ${inverted ? styles.inverted : ''}`

  return (
    <div className={containerClass}>
      <div className={styles.adjustButtons}>
        {buttons.map((amount) => (
          <button
            key={`minus-${amount}`}
            className={styles.adjustButton}
            onClick={() => handleAdjust(-amount)}
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
        <DeltaToast delta={cumulativeDelta} visible={toastVisible} />
      </div>

      <div className={styles.adjustButtons}>
        {buttons.map((amount) => (
          <button
            key={`plus-${amount}`}
            className={styles.adjustButton}
            onClick={() => handleAdjust(amount)}
            aria-label={`Add ${amount} life`}
          >
            +{amount}
          </button>
        ))}
      </div>
    </div>
  )
}
