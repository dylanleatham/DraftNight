import { useState, useMemo } from 'react'
import type { CommanderSession } from '../../types/lifeTracker'
import { CommanderPlayerPanel } from './CommanderPlayerPanel'
import { Modal, Button } from '../ui'
import styles from './CommanderLifeTracker.module.css'

interface CommanderLifeTrackerProps {
  session: CommanderSession
  onAdjustLife: (playerId: string, delta: number) => void
  onSetLife: (playerId: string, life: number) => void
  onAdjustPoison: (playerId: string, delta: number) => void
  onAdjustCommanderDamage: (
    playerId: string,
    fromPlayerId: string,
    delta: number
  ) => void
  onAddMiscCounter: (playerId: string, name: string) => void
  onRemoveMiscCounter: (playerId: string, counterId: string) => void
  onAdjustMiscCounter: (
    playerId: string,
    counterId: string,
    delta: number
  ) => void
  onSetPanelColor?: (playerId: string, color: string) => void
  onSetBackgroundImage?: (
    playerId: string,
    imageUrl: string | undefined
  ) => void
  onResetAll: () => void
  onExit: () => void
}

export function CommanderLifeTracker({
  session,
  onAdjustLife,
  onSetLife,
  onAdjustPoison,
  onAdjustCommanderDamage,
  onAddMiscCounter,
  onRemoveMiscCounter,
  onAdjustMiscCounter,
  onSetPanelColor,
  onSetBackgroundImage,
  onResetAll,
  onExit,
}: CommanderLifeTrackerProps) {
  const [showResetModal, setShowResetModal] = useState(false)

  const playerNames = useMemo(() => {
    const map = new Map<string, string>()
    session.players.forEach((p) => map.set(p.id, p.name))
    return map
  }, [session.players])

  const playerCount = session.players.length
  const gridClass =
    playerCount <= 2
      ? styles.grid2
      : playerCount <= 4
        ? styles.grid4
        : styles.grid6

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <button className={styles.exitButton} onClick={onExit}>
          Exit
        </button>
        <span className={styles.title}>Commander</span>
        <button
          className={styles.resetButton}
          onClick={() => setShowResetModal(true)}
        >
          Reset
        </button>
      </div>

      <div className={`${styles.grid} ${gridClass}`}>
        {session.players.map((player, index) => {
          // Invert top panels for face-to-face play:
          // - 2 players: index 0 (top)
          // - 3-4 players: indices 0-1 (top row)
          const isTopPanel = playerCount <= 2 ? index === 0 : index < 2

          return (
            <CommanderPlayerPanel
              key={player.id}
              player={player}
              playerIndex={index}
              playerNames={playerNames}
              inverted={isTopPanel}
              onAdjustLife={(delta) => onAdjustLife(player.id, delta)}
              onSetLife={(life) => onSetLife(player.id, life)}
              onAdjustPoison={(delta) => onAdjustPoison(player.id, delta)}
              onAdjustCommanderDamage={(fromPlayerId, delta) =>
                onAdjustCommanderDamage(player.id, fromPlayerId, delta)
              }
              onAddMiscCounter={(name) => onAddMiscCounter(player.id, name)}
              onRemoveMiscCounter={(counterId) =>
                onRemoveMiscCounter(player.id, counterId)
              }
              onAdjustMiscCounter={(counterId, delta) =>
                onAdjustMiscCounter(player.id, counterId, delta)
              }
              onSetPanelColor={
                onSetPanelColor
                  ? (color) => onSetPanelColor(player.id, color)
                  : undefined
              }
              onSetBackgroundImage={
                onSetBackgroundImage
                  ? (url) => onSetBackgroundImage(player.id, url)
                  : undefined
              }
            />
          )
        })}
      </div>

      <Modal
        open={showResetModal}
        onClose={() => setShowResetModal(false)}
        title="Reset Game"
      >
        <div className={styles.modalContent}>
          <p>
            Reset all players to starting life totals? This will clear all
            poison counters, commander damage, and custom counters.
          </p>
          <div className={styles.modalActions}>
            <Button
              variant="secondary"
              onClick={() => setShowResetModal(false)}
            >
              Cancel
            </Button>
            <Button
              variant="danger"
              onClick={() => {
                onResetAll()
                setShowResetModal(false)
              }}
            >
              Reset All
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
