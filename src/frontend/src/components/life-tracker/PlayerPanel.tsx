import { useState, useRef, useEffect } from 'react'
import type { DraftPlayer } from '../../types/lifeTracker'
import { BasePlayerPanel, type PlayerColor } from './BasePlayerPanel'
import { LifeDisplay } from './LifeDisplay'
import { PoisonCounter } from './PoisonCounter'
import { GameWinToggle } from './GameWinToggle'
import { MiscCounterList } from './MiscCounter'
import { ColorPicker } from './ColorPicker'
import styles from './PlayerPanel.module.css'

interface PlayerPanelProps {
  player: DraftPlayer
  gameWins: number
  onAdjustLife: (delta: number) => void
  onAdjustPoison: (delta: number) => void
  onToggleWin: () => void
  onAddMiscCounter?: (name: string) => void
  onRemoveMiscCounter?: (counterId: string) => void
  onAdjustMiscCounter?: (counterId: string, delta: number) => void
  onSetPanelColor?: (color: string) => void
  onSetBackgroundImage?: (imageUrl: string | undefined) => void
  color: PlayerColor
  inverted?: boolean
}

export function PlayerPanel({
  player,
  gameWins,
  onAdjustLife,
  onAdjustPoison,
  onToggleWin,
  onAddMiscCounter,
  onRemoveMiscCounter,
  onAdjustMiscCounter,
  onSetPanelColor,
  onSetBackgroundImage,
  color,
  inverted = false,
}: PlayerPanelProps) {
  const [showColorPicker, setShowColorPicker] = useState(false)
  const [isAddingCounter, setIsAddingCounter] = useState(false)
  const [newCounterName, setNewCounterName] = useState('')
  const counterInputRef = useRef<HTMLInputElement>(null)

  useEffect(() => {
    if (isAddingCounter) {
      counterInputRef.current?.focus()
    }
  }, [isAddingCounter])

  const handleSubmitCounter = () => {
    if (newCounterName.trim() && onAddMiscCounter) {
      onAddMiscCounter(newCounterName.trim())
    }
    setNewCounterName('')
    setIsAddingCounter(false)
  }

  const effectiveColor = player.panelColor ?? color
  const hasMiscCounterSupport =
    onAddMiscCounter && onRemoveMiscCounter && onAdjustMiscCounter

  return (
    <>
      <BasePlayerPanel
        name={player.name}
        color={effectiveColor}
        backgroundImage={player.backgroundImage}
        inverted={inverted}
        onNameClick={
          onSetPanelColor ? () => setShowColorPicker(true) : undefined
        }
        headerContent={
          <GameWinToggle
            wins={gameWins}
            onToggle={onToggleWin}
            inverted={inverted}
          />
        }
        footerContent={
          <div className={styles.footerContent}>
            <PoisonCounter poison={player.poison} onAdjust={onAdjustPoison} />
            {hasMiscCounterSupport && player.miscCounters.length > 0 && (
              <MiscCounterList
                counters={player.miscCounters}
                onAdjust={onAdjustMiscCounter}
                onRemove={onRemoveMiscCounter}
                onAdd={onAddMiscCounter}
              />
            )}
            {hasMiscCounterSupport &&
              player.miscCounters.length === 0 &&
              (isAddingCounter ? (
                <input
                  ref={counterInputRef}
                  className={styles.addCounterInput}
                  type="text"
                  placeholder="Counter name"
                  value={newCounterName}
                  onChange={(e) => setNewCounterName(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') handleSubmitCounter()
                    if (e.key === 'Escape') {
                      setNewCounterName('')
                      setIsAddingCounter(false)
                    }
                  }}
                  onBlur={handleSubmitCounter}
                />
              ) : (
                <button
                  className={styles.addCounterButton}
                  onClick={() => setIsAddingCounter(true)}
                >
                  + Counter
                </button>
              ))}
          </div>
        }
      >
        <LifeDisplay
          life={player.life}
          onAdjust={onAdjustLife}
          buttons={[1, 5]}
        />
      </BasePlayerPanel>

      {showColorPicker && onSetPanelColor && (
        <ColorPicker
          currentColor={effectiveColor}
          currentImage={player.backgroundImage}
          archetype={player.archetype}
          onSelectColor={onSetPanelColor}
          onSelectImage={onSetBackgroundImage}
          onClose={() => setShowColorPicker(false)}
        />
      )}
    </>
  )
}
