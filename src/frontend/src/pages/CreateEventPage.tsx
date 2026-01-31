import { useState } from 'react'
import { useNavigate, Link } from 'react-router-dom'
import { Button, Input } from '../components/ui'
import { api, ApiError } from '../api/client'
import { useAuth } from '../context/AuthContext'
import styles from './CreateEventPage.module.css'

export function CreateEventPage() {
  const navigate = useNavigate()
  const { setHostSession, setPlayerSession } = useAuth()

  const [name, setName] = useState('')
  const [packsInBox, setPacksInBox] = useState('36')
  const [hostPin, setHostPin] = useState('')
  const [hostName, setHostName] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isLoading, setIsLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    setError(null)

    // Validation
    if (!name.trim()) {
      setError('Event name is required')
      return
    }
    const packs = parseInt(packsInBox, 10)
    if (isNaN(packs) || packs < 6 || packs > 48) {
      setError('Packs must be between 6 and 48')
      return
    }
    if (!hostPin || hostPin.length < 4) {
      setError('Host PIN must be at least 4 characters')
      return
    }
    if (!hostName.trim()) {
      setError('Your name is required')
      return
    }

    setIsLoading(true)
    try {
      const response = await api.createEvent({
        name: name.trim(),
        packsInBox: packs,
        hostPin,
        hostName: hostName.trim(),
      })

      setHostSession({
        eventId: response.eventId,
        hostToken: response.hostToken,
        joinCode: response.joinCode,
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
        setError('Failed to create event. Please try again.')
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

        <h1 className={styles.title}>Create Event</h1>

        <form onSubmit={handleSubmit} className={styles.form}>
          <Input
            label="Event Name"
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Friday Night Draft"
            maxLength={100}
            autoFocus
          />

          <Input
            label="Packs in Box"
            type="number"
            value={packsInBox}
            onChange={(e) => setPacksInBox(e.target.value)}
            min={6}
            max={48}
          />

          <Input
            label="Your Name"
            value={hostName}
            onChange={(e) => setHostName(e.target.value)}
            placeholder="Your player name"
            maxLength={50}
          />

          <Input
            label="Host PIN"
            type="password"
            value={hostPin}
            onChange={(e) => setHostPin(e.target.value)}
            placeholder="At least 4 characters"
            minLength={4}
            maxLength={20}
          />

          {error && <p className={styles.error}>{error}</p>}

          <Button type="submit" size="large" fullWidth loading={isLoading}>
            Create Event
          </Button>
        </form>
      </div>
    </div>
  )
}
