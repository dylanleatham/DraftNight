import { useCallback, useEffect } from 'react';
import { Outlet, useParams, useNavigate, NavLink } from 'react-router-dom';
import { EventProvider, useEvent } from '../context/EventContext';
import { useEventConnection } from '../hooks/useEventConnection';
import { useEventNotifications } from '../hooks/useEventNotifications';
import { useAuth } from '../context/AuthContext';
import { api } from '../api/client';
import type { EventSnapshotResponse } from '../api/types';
import { EventStatus } from '../api/types';
import styles from './EventLayout.module.css';

function EventLayoutContent() {
  const { eventId } = useParams<{ eventId: string }>();
  const navigate = useNavigate();
  const { state, dispatch, getPlayer } = useEvent();
  const { isHost, getPlayerId } = useAuth();

  const handleSnapshot = useCallback(
    (snapshot: EventSnapshotResponse) => {
      dispatch({ type: 'SET_SNAPSHOT', payload: snapshot });
    },
    [dispatch]
  );

  const handleError = useCallback(
    (code: string, message: string) => {
      dispatch({ type: 'SET_ERROR', payload: message });
      if (code === 'EVENT_NOT_FOUND') {
        navigate('/', { replace: true });
      }
    },
    [dispatch, navigate]
  );

  const { status } = useEventConnection({
    eventId: eventId!,
    onSnapshot: handleSnapshot,
    onError: handleError,
  });

  // Update connection status in state
  useEffect(() => {
    dispatch({ type: 'SET_CONNECTION_STATUS', payload: status });
  }, [status, dispatch]);

  // Watch for event changes and show notifications
  useEventNotifications(state.snapshot, eventId!);

  // Fetch initial snapshot if SignalR takes time to connect
  useEffect(() => {
    if (state.isLoading && !state.snapshot) {
      api
        .getEvent(eventId!)
        .then((snapshot) => {
          dispatch({ type: 'SET_SNAPSHOT', payload: snapshot });
        })
        .catch((err) => {
          dispatch({ type: 'SET_ERROR', payload: err.message });
        });
    }
  }, [eventId, state.isLoading, state.snapshot, dispatch]);

  const isHostUser = isHost(eventId!);
  const playerId = getPlayerId(eventId!);
  const currentPlayer = playerId ? getPlayer(playerId) : null;
  const snapshot = state.snapshot;

  // Show loading state
  if (state.isLoading && !snapshot) {
    return (
      <div className={styles.loading}>
        <div className={styles.spinner} />
        <p>Loading event...</p>
      </div>
    );
  }

  // Show error state
  if (state.error && !snapshot) {
    return (
      <div className={styles.error}>
        <h2>Error</h2>
        <p>{state.error}</p>
        <button onClick={() => navigate('/')}>Go Home</button>
      </div>
    );
  }

  if (!snapshot) {
    return null;
  }

  const showPairings = snapshot.status !== EventStatus.Setup;
  const showPrizes =
    snapshot.status === EventStatus.Completed || snapshot.prizesAllocated;

  return (
    <div className={styles.layout}>
      <header className={styles.header}>
        <div className={styles.headerTop}>
          <h1 className={styles.eventName}>{snapshot.name}</h1>
          <div className={styles.connectionBadge} data-status={status}>
            {status === 'connected' && '●'}
            {status === 'connecting' && '○'}
            {status === 'reconnecting' && '◐'}
            {status === 'disconnected' && '○'}
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
        </div>
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
  );
}

export function EventLayout() {
  return (
    <EventProvider>
      <EventLayoutContent />
    </EventProvider>
  );
}
