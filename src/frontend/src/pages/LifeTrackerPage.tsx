import { useState, useCallback } from 'react'
import { useParams, useSearchParams, useNavigate } from 'react-router-dom'
import { useLifeTracker } from '../hooks/useLifeTracker'
import { useCommanderLifeTracker } from '../hooks/useCommanderLifeTracker'
import { useAuth } from '../context/AuthContext'
import { api } from '../api/client'
import { DraftLifeTracker } from '../components/life-tracker'
import { CommanderLifeTracker } from '../components/life-tracker/CommanderLifeTracker'

export function LifeTrackerPage() {
  const { sessionId } = useParams<{ sessionId: string }>()
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const { isHost, getHostToken } = useAuth()
  const [isExiting, setIsExiting] = useState(false)

  // Parse URL params - route based on player count, not mode
  const playerAName = searchParams.get('playerA') || 'Player 1'
  const playerBName = searchParams.get('playerB') || 'Player 2'
  const playerNames = searchParams.get('players')?.split(',') || []
  const startingLife = parseInt(searchParams.get('startingLife') || '20', 10)

  // Determine if this is a 2-player game (uses draft layout) or 3-4 player (commander layout)
  // 2-player games have playerA/playerB params, 3-4 player games have players param
  const is2PlayerGame = searchParams.has('playerA') || !searchParams.has('players')

  // Event integration params
  const eventId = searchParams.get('eventId') || undefined
  const matchId = searchParams.get('matchId') || undefined
  const playerAId = searchParams.get('playerAId') || undefined
  const playerBId = searchParams.get('playerBId') || undefined

  const handleExit = useCallback(
    async (matchWinner?: 'playerA' | 'playerB') => {
      if (isExiting) return

      // If we have a winner and we're in event mode, finalize the match
      if (eventId && matchId && matchWinner && isHost(eventId)) {
        const winnerId = matchWinner === 'playerA' ? playerAId : playerBId
        if (winnerId) {
          setIsExiting(true)
          try {
            const hostToken = getHostToken(eventId)
            if (hostToken) {
              const snapshot = await api.getEvent(eventId)
              await api.finalizeMatch(
                eventId,
                matchId,
                { hostToken },
                winnerId,
                snapshot.version
              )
            }
          } catch (err) {
            console.error('Failed to finalize match:', err)
          }
          setIsExiting(false)
        }
      }

      // Navigate back
      if (eventId && matchId) {
        navigate(`/event/${eventId}/pairings`)
      } else {
        navigate('/life-tracker')
      }
    },
    [
      eventId,
      matchId,
      playerAId,
      playerBId,
      isHost,
      getHostToken,
      navigate,
      isExiting,
    ]
  )

  if (!sessionId) {
    return null
  }

  // Route based on player count: 2 players use draft layout, 3-4 use commander layout
  if (is2PlayerGame) {
    return (
      <DraftLifeTrackerView
        sessionId={sessionId}
        playerAName={playerAName}
        playerBName={playerBName}
        startingLife={startingLife}
        eventId={eventId}
        matchId={matchId}
        onExit={handleExit}
      />
    )
  }

  return (
    <CommanderLifeTrackerView
      sessionId={sessionId}
      playerNames={playerNames}
      startingLife={startingLife}
      onExit={handleExit}
    />
  )
}

interface DraftLifeTrackerViewProps {
  sessionId: string
  playerAName: string
  playerBName: string
  startingLife: number
  eventId?: string
  matchId?: string
  onExit: (matchWinner?: 'playerA' | 'playerB') => void
}

function DraftLifeTrackerView({
  sessionId,
  playerAName,
  playerBName,
  startingLife,
  eventId,
  matchId,
  onExit,
}: DraftLifeTrackerViewProps) {
  const {
    session,
    adjustLife,
    setLife,
    adjustPoison,
    toggleGameWin,
    resetGame,
    resetMatch,
    addMiscCounter,
    removeMiscCounter,
    adjustMiscCounter,
    setPanelColor,
    setBackgroundImage,
  } = useLifeTracker({
    sessionId,
    playerAName,
    playerBName,
    startingLife,
    eventId,
    matchId,
  })

  return (
    <DraftLifeTracker
      session={session}
      onAdjustLife={adjustLife}
      onSetLife={setLife}
      onAdjustPoison={adjustPoison}
      onToggleWin={toggleGameWin}
      onResetGame={resetGame}
      onResetMatch={resetMatch}
      onExit={onExit}
      onAddMiscCounter={addMiscCounter}
      onRemoveMiscCounter={removeMiscCounter}
      onAdjustMiscCounter={adjustMiscCounter}
      onSetPanelColor={setPanelColor}
      onSetBackgroundImage={setBackgroundImage}
    />
  )
}

interface CommanderLifeTrackerViewProps {
  sessionId: string
  playerNames: string[]
  startingLife: number
  onExit: (matchWinner?: 'playerA' | 'playerB') => void
}

function CommanderLifeTrackerView({
  sessionId,
  playerNames,
  startingLife,
  onExit,
}: CommanderLifeTrackerViewProps) {
  const {
    session,
    adjustLife,
    setLife,
    adjustPoison,
    adjustCommanderDamage,
    addMiscCounter,
    removeMiscCounter,
    adjustMiscCounter,
    resetAll,
    setPanelColor,
    setBackgroundImage,
  } = useCommanderLifeTracker({
    sessionId,
    playerNames,
    startingLife,
  })

  return (
    <CommanderLifeTracker
      session={session}
      onAdjustLife={adjustLife}
      onSetLife={setLife}
      onAdjustPoison={adjustPoison}
      onAdjustCommanderDamage={adjustCommanderDamage}
      onAddMiscCounter={addMiscCounter}
      onRemoveMiscCounter={removeMiscCounter}
      onAdjustMiscCounter={adjustMiscCounter}
      onSetPanelColor={setPanelColor}
      onSetBackgroundImage={setBackgroundImage}
      onResetAll={resetAll}
      onExit={() => onExit()}
    />
  )
}
