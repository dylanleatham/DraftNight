import { useState } from 'react'
import type { DraftSession } from '../../types/lifeTracker'
import { PlayerPanel } from './PlayerPanel'
import { Modal, Button } from '../ui'
import styles from './DraftLifeTracker.module.css'

interface DraftLifeTrackerProps {
  session: DraftSession
  onAdjustLife: (playerId: string, delta: number) => void
  onSetLife: (playerId: string, life: number) => void
  onAdjustPoison: (playerId: string, delta: number) => void
  onToggleWin: (player: 'playerA' | 'playerB') => void
  onResetGame: () => void
  onResetMatch: () => void
  onExit: (matchWinner?: 'playerA' | 'playerB') => void
  onAddMiscCounter?: (playerId: string, name: string) => void
  onRemoveMiscCounter?: (playerId: string, counterId: string) => void
  onAdjustMiscCounter?: (
    playerId: string,
    counterId: string,
    delta: number
  ) => void
  onSetPanelColor?: (playerId: string, color: string) => void
  onSetBackgroundImage?: (
    playerId: string,
    imageUrl: string | undefined
  ) => void
}

export function DraftLifeTracker({
  session,
  onAdjustLife,
  onSetLife,
  onAdjustPoison,
  onToggleWin,
  onResetGame,
  onResetMatch,
  onExit,
  onAddMiscCounter,
  onRemoveMiscCounter,
  onAdjustMiscCounter,
  onSetPanelColor,
  onSetBackgroundImage,
}: DraftLifeTrackerProps) {
  const [showResetModal, setShowResetModal] = useState(false)

  const matchWinnerKey: 'playerA' | 'playerB' | null =
    session.gameWins.playerA >= 2
      ? 'playerA'
      : session.gameWins.playerB >= 2
        ? 'playerB'
        : null
  const matchWinner = matchWinnerKey ? session[matchWinnerKey].name : null

  return (
    <div className={styles.container}>
      {/* Player A (top, inverted for face-to-face) */}
      <PlayerPanel
        player={session.playerA}
        gameWins={session.gameWins.playerA}
        onLifeChange={(life) => onSetLife('playerA', life)}
        onAdjustLife={(delta) => onAdjustLife('playerA', delta)}
        onAdjustPoison={(delta) => onAdjustPoison('playerA', delta)}
        onToggleWin={() => onToggleWin('playerA')}
        onAddMiscCounter={
          onAddMiscCounter
            ? (name) => onAddMiscCounter('playerA', name)
            : undefined
        }
        onRemoveMiscCounter={
          onRemoveMiscCounter
            ? (id) => onRemoveMiscCounter('playerA', id)
            : undefined
        }
        onAdjustMiscCounter={
          onAdjustMiscCounter
            ? (id, delta) => onAdjustMiscCounter('playerA', id, delta)
            : undefined
        }
        onSetPanelColor={
          onSetPanelColor
            ? (color) => onSetPanelColor('playerA', color)
            : undefined
        }
        onSetBackgroundImage={
          onSetBackgroundImage
            ? (url) => onSetBackgroundImage('playerA', url)
            : undefined
        }
        color="blue"
        inverted
      />

      {/* Center bar */}
      <div className={styles.centerBar}>
        <button className={styles.exitButton} onClick={() => onExit()}>
          Exit
        </button>
        <div className={styles.scoreDisplay}>
          <span className={styles.score}>{session.gameWins.playerA}</span>
          <span className={styles.scoreDivider}>-</span>
          <span className={styles.score}>{session.gameWins.playerB}</span>
        </div>
        <button
          className={styles.resetButton}
          onClick={() => setShowResetModal(true)}
        >
          Reset
        </button>
      </div>

      {/* Player B (bottom) */}
      <PlayerPanel
        player={session.playerB}
        gameWins={session.gameWins.playerB}
        onLifeChange={(life) => onSetLife('playerB', life)}
        onAdjustLife={(delta) => onAdjustLife('playerB', delta)}
        onAdjustPoison={(delta) => onAdjustPoison('playerB', delta)}
        onToggleWin={() => onToggleWin('playerB')}
        onAddMiscCounter={
          onAddMiscCounter
            ? (name) => onAddMiscCounter('playerB', name)
            : undefined
        }
        onRemoveMiscCounter={
          onRemoveMiscCounter
            ? (id) => onRemoveMiscCounter('playerB', id)
            : undefined
        }
        onAdjustMiscCounter={
          onAdjustMiscCounter
            ? (id, delta) => onAdjustMiscCounter('playerB', id, delta)
            : undefined
        }
        onSetPanelColor={
          onSetPanelColor
            ? (color) => onSetPanelColor('playerB', color)
            : undefined
        }
        onSetBackgroundImage={
          onSetBackgroundImage
            ? (url) => onSetBackgroundImage('playerB', url)
            : undefined
        }
        color="red"
      />

      {/* Reset Modal */}
      <Modal
        open={showResetModal}
        onClose={() => setShowResetModal(false)}
        title="Reset"
      >
        <div className={styles.modalContent}>
          <p>What would you like to reset?</p>
          <div className={styles.modalActions}>
            <Button
              variant="secondary"
              fullWidth
              onClick={() => {
                onResetGame()
                setShowResetModal(false)
              }}
            >
              Reset Game
            </Button>
            <p className={styles.resetHint}>
              Resets life and poison to replay the game
            </p>
            <Button
              variant="danger"
              fullWidth
              onClick={() => {
                onResetMatch()
                setShowResetModal(false)
              }}
            >
              Reset Match
            </Button>
            <p className={styles.resetHint}>
              Resets everything including game wins
            </p>
          </div>
        </div>
      </Modal>

      {/* Match Winner Overlay */}
      {matchWinner && (
        <div className={styles.winnerOverlay}>
          <div className={styles.winnerContent}>
            <span className={styles.winnerLabel}>Match Winner</span>
            <span className={styles.winnerName}>{matchWinner}</span>
            <div className={styles.winnerActions}>
              <Button
                variant="secondary"
                onClick={() => setShowResetModal(true)}
              >
                Reset
              </Button>
              <Button onClick={() => onExit(matchWinnerKey ?? undefined)}>
                Done
              </Button>
            </div>
          </div>
        </div>
      )}
    </div>
  )
}
