import type { DraftPlayer } from '../../types/lifeTracker';
import { BasePlayerPanel, type PlayerColor } from './BasePlayerPanel';
import { LifeDisplay } from './LifeDisplay';
import { PoisonCounter } from './PoisonCounter';
import { GameWinToggle } from './GameWinToggle';

interface PlayerPanelProps {
  player: DraftPlayer;
  gameWins: number;
  onLifeChange: (life: number) => void;
  onAdjustLife: (delta: number) => void;
  onAdjustPoison: (delta: number) => void;
  onToggleWin: () => void;
  color: PlayerColor;
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
    <BasePlayerPanel
      name={player.name}
      color={color}
      inverted={inverted}
      headerContent={
        <GameWinToggle wins={gameWins} onToggle={onToggleWin} />
      }
      footerContent={
        <PoisonCounter
          poison={player.poison}
          onAdjust={onAdjustPoison}
        />
      }
    >
      <LifeDisplay
        life={player.life}
        onLifeChange={onLifeChange}
        onAdjust={onAdjustLife}
        buttons={[1, 5]}
      />
    </BasePlayerPanel>
  );
}
