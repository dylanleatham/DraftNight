import { useState } from 'react';
import type { CommanderPlayer } from '../../types/lifeTracker';
import { MAX_POISON, COMMANDER_DAMAGE_LETHAL } from '../../types/lifeTracker';
import { LifeDisplay } from './LifeDisplay';
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
  /** Whether this panel should be inverted (rotated 180deg) */
  inverted?: boolean;
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
  inverted = false,
}: CommanderPlayerPanelProps) {
  const [expanded, setExpanded] = useState(false);

  const color = PLAYER_COLORS[playerIndex % PLAYER_COLORS.length];
  const totalCommanderDamage = player.commanderDamage.reduce((sum, cd) => sum + cd.amount, 0);
  const hasLethalCommanderDamage = player.commanderDamage.some(
    (cd) => cd.amount >= COMMANDER_DAMAGE_LETHAL
  );
  const hasLethalPoison = player.poison >= MAX_POISON;

  return (
    <div className={`${styles.container} ${styles[color]} ${inverted ? styles.inverted : ''}`}>
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

        <div className={styles.lifeSection} onClick={(e) => e.stopPropagation()}>
          <LifeDisplay
            life={player.life}
            onLifeChange={onSetLife}
            onAdjust={onAdjustLife}
            buttons={[1, 5]}
            size="compact"
          />
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
