import { useState, type CSSProperties } from 'react'
import type { CommanderPlayer } from '../../types/lifeTracker'
import { MAX_POISON, COMMANDER_DAMAGE_LETHAL } from '../../types/lifeTracker'
import { LifeDisplay } from './LifeDisplay'
import { PoisonCounter } from './PoisonCounter'
import { CommanderDamageTracker } from './CommanderDamageTracker'
import { MiscCounterList } from './MiscCounter'
import { ColorPicker } from './ColorPicker'
import {
  PLAYER_COLORS,
  COLOR_GRADIENTS,
  isPlayerColor,
  adjustBrightness,
} from './panelColors'
import styles from './CommanderPlayerPanel.module.css'

function getBackgroundStyle(
  color: string,
  backgroundImage?: string
): CSSProperties {
  if (backgroundImage) {
    return {
      backgroundImage: `url(${backgroundImage})`,
      backgroundSize: 'contain',
      backgroundPosition: 'center',
      backgroundRepeat: 'no-repeat',
      backgroundColor: 'rgba(0, 0, 0, 0.85)',
    }
  }
  if (isPlayerColor(color)) {
    return { background: COLOR_GRADIENTS[color] }
  }
  return {
    background: `linear-gradient(135deg, ${color} 0%, ${adjustBrightness(color, -30)} 100%)`,
  }
}

interface CommanderPlayerPanelProps {
  player: CommanderPlayer
  playerIndex: number
  playerNames: Map<string, string>
  onAdjustLife: (delta: number) => void
  onAdjustPoison: (delta: number) => void
  onAdjustCommanderDamage: (fromPlayerId: string, delta: number) => void
  onAddMiscCounter: (name: string) => void
  onRemoveMiscCounter: (counterId: string) => void
  onAdjustMiscCounter: (counterId: string, delta: number) => void
  onSetPanelColor?: (color: string) => void
  onSetBackgroundImage?: (imageUrl: string | undefined) => void
  /** Whether this panel should be inverted (rotated 180deg) */
  inverted?: boolean
}

export function CommanderPlayerPanel({
  player,
  playerIndex,
  playerNames,
  onAdjustLife,
  onAdjustPoison,
  onAdjustCommanderDamage,
  onAddMiscCounter,
  onRemoveMiscCounter,
  onAdjustMiscCounter,
  onSetPanelColor,
  onSetBackgroundImage,
  inverted = false,
}: CommanderPlayerPanelProps) {
  const [expanded, setExpanded] = useState(false)
  const [showColorPicker, setShowColorPicker] = useState(false)

  const defaultColor = PLAYER_COLORS[playerIndex % PLAYER_COLORS.length]
  const effectiveColor = player.panelColor ?? defaultColor
  const totalCommanderDamage = player.commanderDamage.reduce(
    (sum, cd) => sum + cd.amount,
    0
  )
  const hasLethalCommanderDamage = player.commanderDamage.some(
    (cd) => cd.amount >= COMMANDER_DAMAGE_LETHAL
  )
  const hasLethalPoison = player.poison >= MAX_POISON
  const backgroundStyle = getBackgroundStyle(
    effectiveColor,
    player.backgroundImage
  )

  const handleNameClick = (e: React.MouseEvent) => {
    if (onSetPanelColor) {
      e.stopPropagation()
      setShowColorPicker(true)
    }
  }

  return (
    <>
      <div
        className={`${styles.container} ${inverted ? styles.inverted : ''}`}
        style={backgroundStyle}
      >
        {/* Compact View */}
        <div
          className={styles.compactView}
          onClick={() => setExpanded(!expanded)}
        >
          <div className={styles.header}>
            <span
              className={`${styles.playerName} ${onSetPanelColor ? styles.clickable : ''}`}
              onClick={handleNameClick}
            >
              {player.name}
              {onSetPanelColor && (
                <span className={styles.editHint}>tap to customize</span>
              )}
            </span>
            <button
              className={styles.expandButton}
              aria-label={expanded ? 'Collapse' : 'Expand'}
            >
              {expanded ? '−' : '+'}
            </button>
          </div>

          <div
            className={styles.lifeSection}
            onClick={(e) => e.stopPropagation()}
          >
            <LifeDisplay
              life={player.life}
              onAdjust={onAdjustLife}
              buttons={[1, 5]}
              size="medium"
            />
          </div>

          <div className={styles.indicators}>
            <div
              className={`${styles.indicator} ${hasLethalPoison ? styles.lethal : ''}`}
              onClick={(e) => e.stopPropagation()}
            >
              <span className={styles.indicatorIcon}>☠</span>
              <span className={styles.indicatorValue}>{player.poison}</span>
            </div>
            {totalCommanderDamage > 0 && (
              <div
                className={`${styles.indicator} ${hasLethalCommanderDamage ? styles.lethal : ''}`}
              >
                <span className={styles.indicatorIcon}>⚔</span>
                <span className={styles.indicatorValue}>
                  {totalCommanderDamage}
                </span>
              </div>
            )}
          </div>
        </div>

        {/* Expanded View */}
        {expanded && (
          <div
            className={styles.expandedView}
            onClick={(e) => e.stopPropagation()}
          >
            <div className={styles.expandedSection}>
              <PoisonCounter
                poison={player.poison}
                onAdjust={onAdjustPoison}
                compact
              />
            </div>

            <CommanderDamageTracker
              commanderDamage={player.commanderDamage}
              playerNames={playerNames}
              onAdjust={onAdjustCommanderDamage}
            />

            <MiscCounterList
              counters={player.miscCounters}
              onAdjust={onAdjustMiscCounter}
              onRemove={onRemoveMiscCounter}
              onAdd={onAddMiscCounter}
            />
          </div>
        )}
      </div>

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
