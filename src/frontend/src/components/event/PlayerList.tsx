import type { PlayerResponse } from '../../api/types';
import { PlayerCard } from './PlayerCard';
import styles from './PlayerList.module.css';

interface PlayerListProps {
  players: PlayerResponse[];
  currentPlayerId?: string | null;
  showStats?: boolean;
  onDropPlayer?: (playerId: string) => void;
}

export function PlayerList({
  players,
  currentPlayerId,
  showStats = false,
  onDropPlayer,
}: PlayerListProps) {
  // Sort by seed
  const sortedPlayers = [...players].sort((a, b) => a.seed - b.seed);

  return (
    <div className={styles.list}>
      {sortedPlayers.map((player) => (
        <PlayerCard
          key={player.id}
          player={player}
          isCurrentUser={player.id === currentPlayerId}
          showStats={showStats}
          onDrop={onDropPlayer ? () => onDropPlayer(player.id) : undefined}
        />
      ))}
    </div>
  );
}
