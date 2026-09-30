import { useState } from 'react'
import { useParams } from 'react-router-dom'
import { Button, Modal } from '../components/ui'
import { PrizeDisplay } from '../components/event'
import { useEvent } from '../context/EventContext'
import { useAuth } from '../context/AuthContext'
import { useHostActions } from '../hooks/useHostActions'
import { EventStatus } from '../api/types'
import { plural } from '../lib/plural'
import styles from './PrizesPage.module.css'

export function PrizesPage() {
  const { eventId } = useParams<{ eventId: string }>()
  const { state } = useEvent()
  const { isHost, getPlayerId } = useAuth()
  const { allocatePrizes } = useHostActions(eventId!)

  const [isAllocating, setIsAllocating] = useState(false)
  const [confirmModalOpen, setConfirmModalOpen] = useState(false)

  const snapshot = state.snapshot
  const isHostUser = isHost(eventId!)
  const playerId = getPlayerId(eventId!)

  if (!snapshot) return null

  const canAllocate =
    isHostUser &&
    !snapshot.prizesAllocated &&
    snapshot.status === EventStatus.Active &&
    snapshot.currentRound === snapshot.totalRounds

  const handleAllocateClick = () => {
    setConfirmModalOpen(true)
  }

  const handleAllocateConfirm = async () => {
    setIsAllocating(true)
    setConfirmModalOpen(false)
    await allocatePrizes()
    setIsAllocating(false)
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h2 className={styles.title}>Prizes</h2>
      </div>

      {snapshot.prizesAllocated ? (
        <PrizeDisplay
          prizeAllocations={snapshot.prizeAllocations}
          players={snapshot.players}
          currentPlayerId={playerId}
          totalPrizePacks={snapshot.prizePacks}
        />
      ) : (
        <div className={styles.notAllocated}>
          <p className={styles.notAllocatedText}>
            Prizes have not been allocated yet.
          </p>
          <p className={styles.prizeInfo}>
            Prize packs available: <strong>{snapshot.prizePacks}</strong>
          </p>
          {canAllocate && (
            <Button
              size="large"
              fullWidth
              loading={isAllocating}
              onClick={handleAllocateClick}
            >
              Allocate Prizes
            </Button>
          )}
          {!canAllocate &&
            isHostUser &&
            snapshot.currentRound < snapshot.totalRounds && (
              <p className={styles.hint}>
                Complete all rounds before allocating prizes.
              </p>
            )}
        </div>
      )}

      <div className={styles.formulaInfo}>
        <h3>Prize Formula</h3>
        <p>Prize packs = Packs in Box - (3 × Players)</p>
        <p>
          {snapshot.packsInBox} - (3 × {snapshot.players.length}) ={' '}
          {plural(snapshot.prizePacks, 'pack')}
        </p>
      </div>

      <Modal
        open={confirmModalOpen}
        onClose={() => setConfirmModalOpen(false)}
        title="Allocate Prizes"
      >
        <div className={styles.modalContent}>
          <p>
            Allocate prizes now? Each match win earns one pack, from a pool of{' '}
            <strong>{plural(snapshot.prizePacks, 'pack')}</strong>. If there
            aren&apos;t enough, later-round winners are paid first.
          </p>
          <p className={styles.modalWarning}>This action cannot be undone.</p>
          <div className={styles.modalActions}>
            <Button
              variant="secondary"
              onClick={() => setConfirmModalOpen(false)}
            >
              Cancel
            </Button>
            <Button onClick={handleAllocateConfirm}>Allocate Prizes</Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
