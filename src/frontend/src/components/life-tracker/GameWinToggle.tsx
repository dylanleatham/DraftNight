import styles from './GameWinToggle.module.css';

interface GameWinToggleProps {
  wins: number;
  maxWins?: number;
  onToggle: () => void;
  inverted?: boolean;
}

export function GameWinToggle({
  wins,
  maxWins = 2,
  onToggle,
  inverted = false,
}: GameWinToggleProps) {
  return (
    <button
      className={`${styles.container} ${inverted ? styles.inverted : ''}`}
      onClick={onToggle}
      aria-label={`${wins} game wins, tap to change`}
    >
      <div className={styles.dots}>
        {Array.from({ length: maxWins }, (_, i) => (
          <span
            key={i}
            className={`${styles.dot} ${i < wins ? styles.won : ''}`}
          />
        ))}
      </div>
    </button>
  );
}
