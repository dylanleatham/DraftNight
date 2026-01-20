import { useState } from 'react';
import type { CommanderPlayer } from '../../types/lifeTracker';
import { MAX_POISON, COMMANDER_DAMAGE_LETHAL } from '../../types/lifeTracker';
import { PoisonCounter } from './PoisonCounter';
import { CommanderDamageTracker } from './CommanderDamageTracker';
import { MiscCounterList } from './MiscCounter';
import styles from './CommanderPlayerPanel.module.css';

const PLAYER_COLORS = ['blue', 'red', 'green', 'purple', 'orange', 'teal'] as const;

interface CommanderPlayerPanelProps {
  player: CommanderPlayer;
  playerIndex: number;
  playerNames: Map<string, string>;
  onAdjustLife: (delta: number) => void;
  onSetLife: (life: number) => void;
  onAdjustPoison: (delta: number) => void;
  onAdjustCommanderDamage: (fromPlayerId: string, delta: number) => void;
  onAddMiscCounter: (name: string) => void;
  onRemoveMiscCounter: (counterId: string) => void;
  onAdjustMiscCounter: (counterId: string, delta: number) => void;
}

export function CommanderPlayerPanel({
  player,
  playerIndex,
  playerNames,
  onAdjustLife,
  onSetLife,
  onAdjustPoison,
  onAdjustCommanderDamage,
  onAddMiscCounter,
  onRemoveMiscCounter,
  onAdjustMiscCounter,
}: CommanderPlayerPanelProps) {
  const [expanded, setExpanded] = useState(false);
  const [isEditingLife, setIsEditingLife] = useState(false);
  const [editValue, setEditValue] = useState('');

  const color = PLAYER_COLORS[playerIndex % PLAYER_COLORS.length];
  const totalCommanderDamage = player.commanderDamage.reduce((sum, cd) => sum + cd.amount, 0);
  const hasLethalCommanderDamage = player.commanderDamage.some(
    (cd) => cd.amount >= COMMANDER_DAMAGE_LETHAL
  );
  const hasLethalPoison = player.poison >= MAX_POISON;

  const handleLifeClick = () => {
    setEditValue(player.life.toString());
    setIsEditingLife(true);
  };

  const handleLifeSubmit = () => {
    const newLife = parseInt(editValue, 10);
    if (!isNaN(newLife)) {
      onSetLife(newLife);
    }
    setIsEditingLife(false);
  };

  const handleKeyDown = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter') {
      handleLifeSubmit();
    } else if (e.key === 'Escape') {
      setIsEditingLife(false);
    }
  };

  return (
    <div className={`${styles.container} ${styles[color]}`}>
      {/* Compact View */}
      <div className={styles.compactView} onClick={() => setExpanded(!expanded)}>
        <div className={styles.header}>
          <span className={styles.playerName}>{player.name}</span>
          <button
            className={styles.expandButton}
            aria-label={expanded ? 'Collapse' : 'Expand'}
          >
            {expanded ? '−' : '+'}
          </button>
        </div>

        <div className={styles.lifeSection}>
          <div className={styles.lifeControls}>
            <button
              className={styles.lifeButton}
              onClick={(e) => {
                e.stopPropagation();
                onAdjustLife(-1);
              }}
            >
              -
            </button>
            <div
              className={styles.lifeDisplay}
              onClick={(e) => {
                e.stopPropagation();
                handleLifeClick();
              }}
            >
              {isEditingLife ? (
                <input
                  type="number"
                  className={styles.lifeInput}
                  value={editValue}
                  onChange={(e) => setEditValue(e.target.value)}
                  onBlur={handleLifeSubmit}
                  onKeyDown={handleKeyDown}
                  onClick={(e) => e.stopPropagation()}
                  autoFocus
                />
              ) : (
                <span className={styles.lifeValue}>{player.life}</span>
              )}
            </div>
            <button
              className={styles.lifeButton}
              onClick={(e) => {
                e.stopPropagation();
                onAdjustLife(1);
              }}
            >
              +
            </button>
          </div>
        </div>

        <div className={styles.indicators}>
          <div
            className={`${styles.indicator} ${hasLethalPoison ? styles.lethal : ''}`}
            onClick={(e) => e.stopPropagation()}
          >
            <span className={styles.indicatorIcon}>☠</span>
            <span className={styles.indicatorValue}>{player.poison}</span>
          </div>
          {totalCommanderDamage > 0 && (
            <div
              className={`${styles.indicator} ${hasLethalCommanderDamage ? styles.lethal : ''}`}
            >
              <span className={styles.indicatorIcon}>⚔</span>
              <span className={styles.indicatorValue}>{totalCommanderDamage}</span>
            </div>
          )}
        </div>
      </div>

      {/* Expanded View */}
      {expanded && (
        <div className={styles.expandedView} onClick={(e) => e.stopPropagation()}>
          <div className={styles.expandedSection}>
            <PoisonCounter
              poison={player.poison}
              onAdjust={onAdjustPoison}
              compact
            />
          </div>

          <CommanderDamageTracker
            commanderDamage={player.commanderDamage}
            playerNames={playerNames}
            onAdjust={onAdjustCommanderDamage}
          />

          <MiscCounterList
            counters={player.miscCounters}
            onAdjust={onAdjustMiscCounter}
            onRemove={onRemoveMiscCounter}
            onAdd={onAddMiscCounter}
          />
        </div>
      )}
    </div>
  );
}
