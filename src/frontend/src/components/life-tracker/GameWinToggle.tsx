import styles from './GameWinToggle.module.css'

interface GameWinToggleProps {
  wins: number
  maxWins?: number
  onToggle: () => void
  inverted?: boolean
}

function TrophyIcon({ won }: { won: boolean }) {
  return (
    <svg
      className={`${styles.trophy} ${won ? styles.won : ''}`}
      viewBox="0 0 24 24"
      fill="currentColor"
      xmlns="http://www.w3.org/2000/svg"
    >
      <path d="M5 4h14v2h-2v1a5 5 0 0 1-3.5 4.77V14h2.5v2H8v-2h2.5v-2.23A5 5 0 0 1 7 7V6H5V4zm4 3a3 3 0 0 0 6 0V6H9v1z" />
      <path d="M3 6h2v3a2 2 0 0 0 2 2h.17A6.98 6.98 0 0 1 5 7V6H3zm16 0h2v1h-2v-1a6.98 6.98 0 0 1-2.17 4H17a2 2 0 0 0 2-2V6z" />
      <rect x="8" y="18" width="8" height="2" rx="1" />
    </svg>
  )
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
      <div className={styles.trophies}>
        {Array.from({ length: maxWins }, (_, i) => (
          <TrophyIcon key={i} won={i < wins} />
        ))}
      </div>
    </button>
  )
}
