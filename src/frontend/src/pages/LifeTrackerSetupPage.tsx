import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { Button, Input, Card } from '../components/ui'
import { lifeTrackerStorage } from '../lib/lifeTrackerStorage'
import type { LifeTrackerSession } from '../types/lifeTracker'
import { getRandomArchetypes } from '../lib/archetypeImages'
import styles from './LifeTrackerSetupPage.module.css'

function generateSessionId(): string {
  return `lt_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`
}

function formatSessionTime(timestamp: number): string {
  const date = new Date(timestamp)
  const now = new Date()
  const diff = now.getTime() - timestamp

  if (diff < 60000) return 'Just now'
  if (diff < 3600000) return `${Math.floor(diff / 60000)}m ago`
  if (diff < 86400000) return `${Math.floor(diff / 3600000)}h ago`

  return date.toLocaleDateString()
}

function getSessionDescription(session: LifeTrackerSession): string {
  if (session.mode === 'draft') {
    return `${session.playerA.name} vs ${session.playerB.name}`
  }
  return `${session.players.length} players`
}

function getSessionLifeInfo(session: LifeTrackerSession): string {
  return `${session.startingLife} life`
}

export function LifeTrackerSetupPage() {
  const navigate = useNavigate()

  const [playerCount, setPlayerCount] = useState(2)
  const [startingLife, setStartingLife] = useState(20)
  const [playerNames, setPlayerNames] = useState<string[]>(['', '', '', ''])
  const [sessionsVersion, setSessionsVersion] = useState(0)

  // Re-fetch sessions when version changes (after delete)
  // sessionsVersion dependency ensures component re-renders when sessions are deleted
  void sessionsVersion
  const recentSessions = lifeTrackerStorage.getRecentSessions()

  const handleStartGame = () => {
    const sessionId = generateSessionId()
    const names = playerNames
      .slice(0, playerCount)
      .map((name, i) => name || `Player ${i + 1}`)

    // Pre-assign unique archetypes for all players
    const archetypes = getRandomArchetypes(playerCount)

    if (playerCount === 2) {
      // 2-player games use draft layout
      const params = new URLSearchParams({
        playerA: names[0],
        playerB: names[1],
        startingLife: startingLife.toString(),
        archetypeA: archetypes[0],
        archetypeB: archetypes[1],
      })
      navigate(`/life-tracker/game/${sessionId}?${params.toString()}`)
    } else {
      // 3-4 player games use commander layout
      const params = new URLSearchParams({
        players: names.join(','),
        startingLife: startingLife.toString(),
        archetypes: archetypes.join(','),
      })
      navigate(`/life-tracker/game/${sessionId}?${params.toString()}`)
    }
  }

  const handleResumeSession = (session: LifeTrackerSession) => {
    // Resume sessions based on their mode
    if (session.mode === 'draft') {
      navigate(`/life-tracker/game/${session.id}?playerA=${encodeURIComponent(session.playerA.name)}&playerB=${encodeURIComponent(session.playerB.name)}`)
    } else {
      const playerNamesParam = session.players.map((p) => p.name).join(',')
      navigate(`/life-tracker/game/${session.id}?players=${encodeURIComponent(playerNamesParam)}`)
    }
  }

  const handleDeleteSession = (session: LifeTrackerSession) => {
    if (session.mode === 'draft') {
      lifeTrackerStorage.deleteDraftSession(session.id)
    } else {
      lifeTrackerStorage.deleteCommanderSession(session.id)
    }
    // Force re-render by incrementing version
    setSessionsVersion((v) => v + 1)
  }

  const handlePlayerNameChange = (index: number, name: string) => {
    const newNames = [...playerNames]
    newNames[index] = name
    setPlayerNames(newNames)
  }

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <Link to="/" className={styles.backLink}>
          &larr; Back
        </Link>

        <h1 className={styles.title}>Life Tracker</h1>

        <div className={styles.setupForm}>
          {/* Player Count Selector */}
          <div className={styles.selectorGroup}>
            <label className={styles.label}>Number of Players</label>
            <div className={styles.selectorButtons}>
              {[2, 3, 4].map((count) => (
                <button
                  key={count}
                  className={`${styles.selectorButton} ${playerCount === count ? styles.active : ''}`}
                  onClick={() => setPlayerCount(count)}
                >
                  {count}
                </button>
              ))}
            </div>
          </div>

          {/* Starting Life Selector */}
          <div className={styles.selectorGroup}>
            <label className={styles.label}>Starting Life</label>
            <div className={styles.selectorButtons}>
              {[20, 30, 40].map((life) => (
                <button
                  key={life}
                  className={`${styles.selectorButton} ${startingLife === life ? styles.active : ''}`}
                  onClick={() => setStartingLife(life)}
                >
                  {life}
                </button>
              ))}
            </div>
          </div>

          {/* Player Names */}
          {Array.from({ length: playerCount }, (_, i) => (
            <Input
              key={i}
              label={`Player ${i + 1} Name`}
              value={playerNames[i]}
              onChange={(e) => handlePlayerNameChange(i, e.target.value)}
              placeholder={`Player ${i + 1}`}
            />
          ))}

          <Button size="large" fullWidth onClick={handleStartGame}>
            Start Game
          </Button>
        </div>

        {/* Recent Sessions */}
        {recentSessions.length > 0 && (
          <div className={styles.recentSessions}>
            <h2 className={styles.sectionTitle}>Recent Sessions</h2>
            <div className={styles.sessionList}>
              {recentSessions.map((session) => (
                <Card key={session.id} className={styles.sessionCard}>
                  <div className={styles.sessionInfo}>
                    <span className={styles.sessionMeta}>
                      {session.mode === 'draft' ? '2 players' : `${session.players.length} players`} &bull; {getSessionLifeInfo(session)}
                    </span>
                    <span className={styles.sessionDescription}>
                      {getSessionDescription(session)}
                    </span>
                    <span className={styles.sessionTime}>
                      {formatSessionTime(session.updatedAt)}
                    </span>
                  </div>
                  <div className={styles.sessionActions}>
                    <Button
                      size="small"
                      onClick={() => handleResumeSession(session)}
                    >
                      Resume
                    </Button>
                    <Button
                      size="small"
                      variant="ghost"
                      onClick={() => handleDeleteSession(session)}
                    >
                      Delete
                    </Button>
                  </div>
                </Card>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
