import type { DraftPlayer } from '../../types/lifeTracker';
import { LifeCounter } from './LifeCounter';
import { PoisonCounter } from './PoisonCounter';
import { GameWinToggle } from './GameWinToggle';
import styles from './PlayerPanel.module.css';

interface PlayerPanelProps {
  player: DraftPlayer;
  gameWins: number;
  onLifeChange: (life: number) => void;
  onAdjustLife: (delta: number) => void;
  onAdjustPoison: (delta: number) => void;
  onToggleWin: () => void;
  color: 'blue' | 'red' | 'green' | 'purple' | 'orange' | 'teal';
  inverted?: boolean;
}

export function PlayerPanel({
  player,
  gameWins,
  onLifeChange,
  onAdjustLife,
  onAdjustPoison,
  onToggleWin,
  color,
  inverted = false,
}: PlayerPanelProps) {
  return (
    <div
      className={`${styles.container} ${styles[color]} ${inverted ? styles.inverted : ''}`}
    >
      <div className={styles.header}>
        <span className={styles.playerName}>{player.name}</span>
        <GameWinToggle wins={gameWins} onToggle={onToggleWin} inverted={inverted} />
      </div>
      <LifeCounter
        life={player.life}
        onLifeChange={onLifeChange}
        onAdjust={onAdjustLife}
        color={color}
        inverted={inverted}
      />
      <div className={styles.footer}>
        <PoisonCounter
          poison={player.poison}
          onAdjust={onAdjustPoison}
          inverted={inverted}
        />
      </div>
    </div>
  );
}
