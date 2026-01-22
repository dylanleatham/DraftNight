import { useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import { Button, Input, Card } from '../components/ui';
import { lifeTrackerStorage } from '../lib/lifeTrackerStorage';
import type { TrackerMode, LifeTrackerSession } from '../types/lifeTracker';
import styles from './LifeTrackerSetupPage.module.css';

function generateSessionId(): string {
  return `lt_${Date.now()}_${Math.random().toString(36).substring(2, 9)}`;
}

function formatSessionTime(timestamp: number): string {
  const date = new Date(timestamp);
  const now = new Date();
  const diff = now.getTime() - timestamp;

  if (diff < 60000) return 'Just now';
  if (diff < 3600000) return `${Math.floor(diff / 60000)}m ago`;
  if (diff < 86400000) return `${Math.floor(diff / 3600000)}h ago`;

  return date.toLocaleDateString();
}

function getSessionDescription(session: LifeTrackerSession): string {
  if (session.mode === 'draft') {
    return `${session.playerA.name} vs ${session.playerB.name}`;
  }
  return `${session.players.length} players`;
}

export function LifeTrackerSetupPage() {
  const navigate = useNavigate();

  const [mode, setMode] = useState<TrackerMode>('draft');
  const [playerAName, setPlayerAName] = useState('');
  const [playerBName, setPlayerBName] = useState('');
  const [playerCount, setPlayerCount] = useState(4);
  const [playerNames, setPlayerNames] = useState<string[]>(['', '', '', '']);
  const [sessionsVersion, setSessionsVersion] = useState(0);

  // Re-fetch sessions when version changes (after delete)
  const recentSessions = lifeTrackerStorage.getRecentSessions();
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  const _forceUpdate = sessionsVersion; // Ensures re-render on delete

  const handleStartDraft = () => {
    const sessionId = generateSessionId();
    const params = new URLSearchParams({
      mode: 'draft',
      playerA: playerAName || 'Player 1',
      playerB: playerBName || 'Player 2',
    });
    navigate(`/life-tracker/game/${sessionId}?${params.toString()}`);
  };

  const handleStartCommander = () => {
    const sessionId = generateSessionId();
    const names = playerNames.slice(0, playerCount).map(
      (name, i) => name || `Player ${i + 1}`
    );
    const params = new URLSearchParams({
      mode: 'commander',
      players: names.join(','),
    });
    navigate(`/life-tracker/game/${sessionId}?${params.toString()}`);
  };

  const handleResumeSession = (session: LifeTrackerSession) => {
    navigate(`/life-tracker/game/${session.id}?mode=${session.mode}`);
  };

  const handleDeleteSession = (session: LifeTrackerSession) => {
    if (session.mode === 'draft') {
      lifeTrackerStorage.deleteDraftSession(session.id);
    } else {
      lifeTrackerStorage.deleteCommanderSession(session.id);
    }
    // Force re-render by incrementing version
    setSessionsVersion((v) => v + 1);
  };

  const handlePlayerNameChange = (index: number, name: string) => {
    const newNames = [...playerNames];
    newNames[index] = name;
    setPlayerNames(newNames);
  };

  return (
    <div className={styles.container}>
      <div className={styles.content}>
        <Link to="/" className={styles.backLink}>
          &larr; Back
        </Link>

        <h1 className={styles.title}>Life Tracker</h1>

        {/* Mode Selection */}
        <div className={styles.modeSelector}>
          <button
            className={`${styles.modeButton} ${mode === 'draft' ? styles.active : ''}`}
            onClick={() => setMode('draft')}
          >
            Draft (1v1)
          </button>
          <button
            className={`${styles.modeButton} ${mode === 'commander' ? styles.active : ''}`}
            onClick={() => setMode('commander')}
          >
            Commander
          </button>
        </div>

        {/* Draft Setup */}
        {mode === 'draft' && (
          <div className={styles.setupForm}>
            <Input
              label="Player 1 Name"
              value={playerAName}
              onChange={(e) => setPlayerAName(e.target.value)}
              placeholder="Player 1"
            />
            <Input
              label="Player 2 Name"
              value={playerBName}
              onChange={(e) => setPlayerBName(e.target.value)}
              placeholder="Player 2"
            />
            <Button size="large" fullWidth onClick={handleStartDraft}>
              Start Draft Game
            </Button>
          </div>
        )}

        {/* Commander Setup */}
        {mode === 'commander' && (
          <div className={styles.setupForm}>
            <div className={styles.playerCountSelector}>
              <label className={styles.label}>Number of Players</label>
              <div className={styles.playerCountButtons}>
                {[2, 3, 4].map((count) => (
                  <button
                    key={count}
                    className={`${styles.countButton} ${playerCount === count ? styles.active : ''}`}
                    onClick={() => setPlayerCount(count)}
                  >
                    {count}
                  </button>
                ))}
              </div>
            </div>

            {Array.from({ length: playerCount }, (_, i) => (
              <Input
                key={i}
                label={`Player ${i + 1} Name`}
                value={playerNames[i]}
                onChange={(e) => handlePlayerNameChange(i, e.target.value)}
                placeholder={`Player ${i + 1}`}
              />
            ))}

            <Button size="large" fullWidth onClick={handleStartCommander}>
              Start Commander Game
            </Button>
          </div>
        )}

        {/* Recent Sessions */}
        {recentSessions.length > 0 && (
          <div className={styles.recentSessions}>
            <h2 className={styles.sectionTitle}>Recent Sessions</h2>
            <div className={styles.sessionList}>
              {recentSessions.map((session) => (
                <Card key={session.id} className={styles.sessionCard}>
                  <div className={styles.sessionInfo}>
                    <span className={styles.sessionMode}>
                      {session.mode === 'draft' ? 'Draft' : 'Commander'}
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
  );
}
