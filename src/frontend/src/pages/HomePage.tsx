import { useNavigate } from 'react-router-dom';
import { useEffect } from 'react';
import { Button } from '../components/ui';
import { useAuth } from '../context/AuthContext';
import styles from './HomePage.module.css';

export function HomePage() {
  const navigate = useNavigate();
  const { hostSession, playerSession } = useAuth();

  // Check for existing session and redirect
  useEffect(() => {
    if (hostSession) {
      navigate(`/event/${hostSession.eventId}/lobby`, { replace: true });
    } else if (playerSession) {
      navigate(`/event/${playerSession.eventId}/lobby`, { replace: true });
    }
  }, [hostSession, playerSession, navigate]);

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <h1 className={styles.title}>MTG Draft Night</h1>
        <p className={styles.subtitle}>
          Manage your in-person Magic: The Gathering draft events
        </p>

        <div className={styles.actions}>
          <Button
            size="large"
            fullWidth
            onClick={() => navigate('/create')}
          >
            Create Event
          </Button>
          <Button
            variant="secondary"
            size="large"
            fullWidth
            onClick={() => navigate('/join')}
          >
            Join Event
          </Button>
        </div>

        <div className={styles.tools}>
          <Button
            variant="ghost"
            size="medium"
            onClick={() => navigate('/life-tracker')}
          >
            Life Tracker
          </Button>
        </div>
      </div>
    </div>
  );
}
