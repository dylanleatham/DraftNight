import { useState } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { Button, Input } from '../components/ui'
import { api, ApiError } from '../api/client'
import { useAuth } from '../context/AuthContext'
import styles from './JoinEventPage.module.css'

export function JoinEventPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { setPlayerSession, setHostSession } = useAuth()
  // Rejoin reclaims an existing seat (new phone, cleared browser) with name + PIN
  const [isRejoin, setIsRejoin] = useState(
    () => searchParams.get('mode') === 'rejoin'
  )

  const [joinCode, setJoinCode] = useState(
    () => searchParams.get('code')?.toUpperCase() ?? ''
  )
  const [playerName, setPlayerName] = useState('')
  const [playerPin, setPlayerPin] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    // Validation
    if (!joinCode.trim() || joinCode.length < 4) {
      setError('Join code is required')
      return
    }
    if (!playerName.trim()) {
      setError('Player name is required')
      return
    }
    if (!playerPin || playerPin.length < 4) {
      setError('Player PIN must be at least 4 characters')
      return
    }

    setIsLoading(true)
    try {
      if (isRejoin) {
        const response = await api.resumeSession({
          joinCode: joinCode.trim().toUpperCase(),
          playerName: playerName.trim(),
          pin: playerPin,
        })

        setPlayerSession({
          eventId: response.eventId,
          playerId: response.playerId,
          playerToken: response.playerToken,
        })
        if (response.hostToken) {
          setHostSession({
            eventId: response.eventId,
            hostToken: response.hostToken,
            joinCode: response.joinCode,
          })
        }

        navigate(`/event/${response.eventId}`)
        return
      }

      const response = await api.joinEvent({
        joinCode: joinCode.trim().toUpperCase(),
        playerName: playerName.trim(),
        playerPin,
      })

      setPlayerSession({
        eventId: response.eventId,
        playerId: response.playerId,
        playerToken: response.playerToken,
      })

      navigate(`/event/${response.eventId}/lobby`)
    } catch (err) {
      if (err instanceof ApiError && err.status === 429) {
        setError('Too many attempts. Wait a minute and try again.')
      } else if (err instanceof ApiError) {
        setError(err.message)
      } else {
        setError('Failed to join event. Please check the code and try again.')
      }
    } finally {
      setIsLoading(false)
    }
  }

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <Link to="/" className={styles.backLink}>
          &larr; Back
        </Link>

        <h1 className={styles.title}>
          {isRejoin ? 'Rejoin Event' : 'Join Event'}
        </h1>

        {isRejoin && (
          <p className={styles.hint}>
            Use the name and PIN you joined with. Hosts use the host PIN. Your
            other device will be signed out.
          </p>
        )}

        <form onSubmit={handleSubmit} className={styles.form}>
          <Input
            label="Join Code"
            value={joinCode}
            onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
            placeholder="ABC123"
            maxLength={10}
            autoFocus={!joinCode}
            style={{ textTransform: 'uppercase', fontFamily: 'monospace' }}
          />

          <Input
            label="Your Name"
            value={playerName}
            onChange={(e) => setPlayerName(e.target.value)}
            placeholder="Enter your name"
            maxLength={50}
            autoFocus={!!joinCode}
          />

          <Input
            label="Your PIN"
            type="password"
            value={playerPin}
            onChange={(e) => setPlayerPin(e.target.value)}
            placeholder="At least 4 characters"
            minLength={4}
            maxLength={20}
          />

          {error && <p className={styles.error}>{error}</p>}

          <Button type="submit" size="large" fullWidth loading={isLoading}>
            {isRejoin ? 'Rejoin Event' : 'Join Event'}
          </Button>
        </form>

        <button
          type="button"
          className={styles.modeToggle}
          onClick={() => {
            setIsRejoin((value) => !value)
            setError(null)
          }}
        >
          {isRejoin
            ? 'New to this event? Join instead'
            : 'Already joined on another device? Rejoin'}
        </button>
      </div>
    </div>
  )
}
