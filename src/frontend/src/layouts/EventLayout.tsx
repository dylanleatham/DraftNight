import { useCallback, useEffect, useState } from 'react'
import { Outlet, useParams, useNavigate, NavLink } from 'react-router-dom'
import { EventProvider, useEvent } from '../context/EventContext'
import { useEventConnection } from '../hooks/useEventConnection'
import { useEventNotifications } from '../hooks/useEventNotifications'
import { useAuth } from '../context/AuthContext'
import { api } from '../api/client'
import type { EventSnapshotResponse } from '../api/types'
import { EventStatus, RoundStatus } from '../api/types'
import styles from './EventLayout.module.css'

function EventLayoutContent() {
  const { eventId } = useParams<{ eventId: string }>()
  const navigate = useNavigate()
  const { state, dispatch, getPlayer } = useEvent()
  const { isHost, getPlayerId, getHostToken, clearAll } = useAuth()
  const [showCancelConfirm, setShowCancelConfirm] = useState(false)
  const [isCancelling, setIsCancelling] = useState(false)

  const handleSnapshot = useCallback(
    (snapshot: EventSnapshotResponse) => {
      dispatch({ type: 'SET_SNAPSHOT', payload: snapshot })
    },
    [dispatch]
  )

  const handleError = useCallback(
    (code: string, message: string) => {
      dispatch({ type: 'SET_ERROR', payload: message })
      if (code === 'EVENT_NOT_FOUND') {
        navigate('/', { replace: true })
      }
    },
    [dispatch, navigate]
  )

  const handleCancelled = useCallback(() => {
    // Event was cancelled (either by us or by notification from server)
    clearAll()
    navigate('/', { replace: true })
  }, [clearAll, navigate])

  const { status } = useEventConnection({
    eventId: eventId!,
    onSnapshot: handleSnapshot,
    onError: handleError,
    onCancelled: handleCancelled,
  })

  const handleCancelEvent = async () => {
    if (!eventId || !state.snapshot) return

    const hostToken = getHostToken(eventId)
    if (!hostToken) return

    setIsCancelling(true)
    try {
      const response = await api.cancelEvent(
        eventId,
        hostToken,
        state.snapshot.version
      )
      if (response.success) {
        // The SignalR EventCancelled message will trigger navigation
        // but we also clear and navigate here for immediate feedback
        clearAll()
        navigate('/', { replace: true })
      } else {
        dispatch({ type: 'SET_ERROR', payload: response.error || 'Failed to cancel event' })
      }
    } catch {
      dispatch({ type: 'SET_ERROR', payload: 'Failed to cancel event' })
    } finally {
      setIsCancelling(false)
      setShowCancelConfirm(false)
    }
  }

  // Update connection status in state
  useEffect(() => {
    dispatch({ type: 'SET_CONNECTION_STATUS', payload: status })
  }, [status, dispatch])

  // Watch for event changes and show notifications
  useEventNotifications(state.snapshot, eventId!)

  // Fetch initial snapshot if SignalR takes time to connect
  useEffect(() => {
    if (state.isLoading && !state.snapshot) {
      api
        .getEvent(eventId!)
        .then((snapshot) => {
          dispatch({ type: 'SET_SNAPSHOT', payload: snapshot })
        })
        .catch((err) => {
          dispatch({ type: 'SET_ERROR', payload: err.message })
        })
    }
  }, [eventId, state.isLoading, state.snapshot, dispatch])

  const isHostUser = isHost(eventId!)
  const playerId = getPlayerId(eventId!)
  const currentPlayer = playerId ? getPlayer(playerId) : null
  const snapshot = state.snapshot

  // Show loading state
  if (state.isLoading && !snapshot) {
    return (
      <div className={styles.loading}>
        <div className={styles.spinner} />
        <p>Loading event...</p>
      </div>
    )
  }

  // Show error state
  if (state.error && !snapshot) {
    return (
      <div className={styles.error}>
        <h2>Error</h2>
        <p>{state.error}</p>
        <button onClick={() => navigate('/')}>Go Home</button>
      </div>
    )
  }

  if (!snapshot) {
    return null
  }

  const showPairings = snapshot.status !== EventStatus.Setup
  const currentRound = snapshot.rounds.find(
    (r) => r.roundNumber === snapshot.currentRound
  )
  const isTournamentReady =
    snapshot.currentRound === snapshot.totalRounds &&
    currentRound?.status === RoundStatus.Closed
  const showPrizes =
    snapshot.status === EventStatus.Completed ||
    snapshot.prizesAllocated ||
    isTournamentReady

  return (
    <div className={styles.layout}>
      <header className={styles.header}>
        <div className={styles.headerTop}>
          <h1 className={styles.eventName}>{snapshot.name}</h1>
          <div
            className={styles.connectionBadge}
            data-status={status}
            title={
              status === 'connected'
                ? 'Live - updates sync automatically'
                : status === 'connecting'
                  ? 'Connecting to server...'
                  : status === 'reconnecting'
                    ? 'Reconnecting...'
                    : 'Offline - changes may not sync'
            }
          >
            <span className={styles.connectionDot}>
              {status === 'connected' && '●'}
              {status === 'connecting' && '○'}
              {status === 'reconnecting' && '◐'}
              {status === 'disconnected' && '○'}
            </span>
            <span className={styles.connectionLabel}>
              {status === 'connected' && 'Live'}
              {status === 'connecting' && 'Connecting'}
              {status === 'reconnecting' && 'Reconnecting'}
              {status === 'disconnected' && 'Offline'}
            </span>
          </div>
        </div>
        <div className={styles.headerInfo}>
          {isHostUser && <span className={styles.hostBadge}>Host</span>}
          {currentPlayer && (
            <span className={styles.playerName}>{currentPlayer.name}</span>
          )}
          {snapshot.joinCode && (
            <span className={styles.joinCode}>Code: {snapshot.joinCode}</span>
          )}
          {isHostUser && (
            <button
              className={styles.cancelButton}
              onClick={() => setShowCancelConfirm(true)}
              disabled={isCancelling}
            >
              Cancel Event
            </button>
          )}
        </div>

        {showCancelConfirm && (
          <div className={styles.confirmOverlay}>
            <div className={styles.confirmDialog}>
              <h3>Cancel Event?</h3>
              <p>
                This will end the event and remove all players. This action
                cannot be undone.
              </p>
              <div className={styles.confirmButtons}>
                <button
                  className={styles.confirmCancel}
                  onClick={() => setShowCancelConfirm(false)}
                  disabled={isCancelling}
                >
                  Keep Event
                </button>
                <button
                  className={styles.confirmDelete}
                  onClick={handleCancelEvent}
                  disabled={isCancelling}
                >
                  {isCancelling ? 'Cancelling...' : 'Cancel Event'}
                </button>
              </div>
            </div>
          </div>
        )}
      </header>

      <nav className={styles.nav}>
        <NavLink
          to={`/event/${eventId}/lobby`}
          className={({ isActive }) =>
            `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
          }
        >
          Lobby
        </NavLink>
        {showPairings && (
          <NavLink
            to={`/event/${eventId}/pairings`}
            className={({ isActive }) =>
              `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
            }
          >
            Pairings
          </NavLink>
        )}
        {showPairings && (
          <NavLink
            to={`/event/${eventId}/standings`}
            className={({ isActive }) =>
              `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
            }
          >
            Standings
          </NavLink>
        )}
        {showPrizes && (
          <NavLink
            to={`/event/${eventId}/prizes`}
            className={({ isActive }) =>
              `${styles.navLink} ${isActive ? styles.navLinkActive : ''}`
            }
          >
            Prizes
          </NavLink>
        )}
        {isHostUser && (
          <NavLink
            to={`/event/${eventId}/audit`}
            className={({ isActive }) =>
              `${styles.navLink} ${styles.navLinkHost} ${isActive ? styles.navLinkActive : ''}`
            }
          >
            Audit
          </NavLink>
        )}
      </nav>

      <main className={styles.main}>
        <Outlet />
      </main>
    </div>
  )
}

export function EventLayout() {
  return (
    <EventProvider>
      <EventLayoutContent />
    </EventProvider>
  )
}
