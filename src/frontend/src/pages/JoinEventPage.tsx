import { useState, useEffect } from 'react'
import { useNavigate, useSearchParams, Link } from 'react-router-dom'
import { Button, Input } from '../components/ui'
import { api, ApiError } from '../api/client'
import { useAuth } from '../context/AuthContext'
import styles from './JoinEventPage.module.css'

export function JoinEventPage() {
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const { setPlayerSession } = useAuth()

  const [joinCode, setJoinCode] = useState(searchParams.get('code') ?? '')
  const [playerName, setPlayerName] = useState('')
  const [playerPin, setPlayerPin] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  // Pre-fill join code from URL
  useEffect(() => {
    const code = searchParams.get('code')
    if (code) {
      setJoinCode(code.toUpperCase())
    }
  }, [searchParams])

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
      if (err instanceof ApiError) {
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

        <h1 className={styles.title}>Join Event</h1>

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
            Join Event
          </Button>
        </form>
      </div>
    </div>
  )
}
