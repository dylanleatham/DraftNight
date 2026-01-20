import { useParams, useSearchParams, useNavigate } from 'react-router-dom';
import { useLifeTracker } from '../hooks/useLifeTracker';
import { useCommanderLifeTracker } from '../hooks/useCommanderLifeTracker';
import { DraftLifeTracker } from '../components/life-tracker';
import { CommanderLifeTracker } from '../components/life-tracker/CommanderLifeTracker';
import type { TrackerMode } from '../types/lifeTracker';

export function LifeTrackerPage() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const mode = (searchParams.get('mode') || 'draft') as TrackerMode;
  const playerAName = searchParams.get('playerA') || 'Player 1';
  const playerBName = searchParams.get('playerB') || 'Player 2';
  const playerNames = searchParams.get('players')?.split(',') || [];

  // Event integration params
  const eventId = searchParams.get('eventId') || undefined;
  const matchId = searchParams.get('matchId') || undefined;

  const handleExit = () => {
    if (eventId && matchId) {
      navigate(`/event/${eventId}/pairings`);
    } else {
      navigate('/life-tracker');
    }
  };

  if (!sessionId) {
    return null;
  }

  if (mode === 'draft') {
    return (
      <DraftLifeTrackerView
        sessionId={sessionId}
        playerAName={playerAName}
        playerBName={playerBName}
        eventId={eventId}
        matchId={matchId}
        onExit={handleExit}
      />
    );
  }

  return (
    <CommanderLifeTrackerView
      sessionId={sessionId}
      playerNames={playerNames}
      onExit={handleExit}
    />
  );
}

interface DraftLifeTrackerViewProps {
  sessionId: string;
  playerAName: string;
  playerBName: string;
  eventId?: string;
  matchId?: string;
  onExit: () => void;
}

function DraftLifeTrackerView({
  sessionId,
  playerAName,
  playerBName,
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
  } = useLifeTracker({
    sessionId,
    playerAName,
    playerBName,
    eventId,
    matchId,
  });

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
    />
  );
}

interface CommanderLifeTrackerViewProps {
  sessionId: string;
  playerNames: string[];
  onExit: () => void;
}

function CommanderLifeTrackerView({
  sessionId,
  playerNames,
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
  } = useCommanderLifeTracker({
    sessionId,
    playerNames,
  });

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
      onResetAll={resetAll}
      onExit={onExit}
    />
  );
}
