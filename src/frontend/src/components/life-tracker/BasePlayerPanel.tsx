import type { ReactNode, CSSProperties } from 'react'
import styles from './BasePlayerPanel.module.css'

export type PlayerColor =
  | 'blue'
  | 'red'
  | 'green'
  | 'purple'
  | 'orange'
  | 'teal'

const COLOR_GRADIENTS: Record<PlayerColor, string> = {
  blue: 'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)',
  red: 'linear-gradient(135deg, #ef4444 0%, #b91c1c 100%)',
  green: 'linear-gradient(135deg, #22c55e 0%, #15803d 100%)',
  purple: 'linear-gradient(135deg, #a855f7 0%, #7e22ce 100%)',
  orange: 'linear-gradient(135deg, #f97316 0%, #c2410c 100%)',
  teal: 'linear-gradient(135deg, #14b8a6 0%, #0f766e 100%)',
}

function isPlayerColor(color: string): color is PlayerColor {
  return ['blue', 'red', 'green', 'purple', 'orange', 'teal'].includes(color)
}

function getBackgroundStyle(
  color: string,
  backgroundImage?: string
): CSSProperties {
  // If there's a background image, use contain to show the full character
  if (backgroundImage) {
    return {
      backgroundImage: `url(${backgroundImage})`,
      backgroundSize: 'contain',
      backgroundPosition: 'center',
      backgroundRepeat: 'no-repeat',
      backgroundColor: 'rgba(0, 0, 0, 0.85)',
    }
  }

  // If it's a preset color name, use the gradient
  if (isPlayerColor(color)) {
    return { background: COLOR_GRADIENTS[color] }
  }

  // Otherwise it's a custom hex color - create a gradient from it
  return {
    background: `linear-gradient(135deg, ${color} 0%, ${adjustBrightness(color, -30)} 100%)`,
  }
}

function adjustBrightness(hex: string, percent: number): string {
  // Ensure hex starts with #
  const cleanHex = hex.startsWith('#') ? hex.slice(1) : hex

  const num = parseInt(cleanHex, 16)
  const r = Math.max(0, Math.min(255, (num >> 16) + percent))
  const g = Math.max(0, Math.min(255, ((num >> 8) & 0x00ff) + percent))
  const b = Math.max(0, Math.min(255, (num & 0x0000ff) + percent))

  return `#${((r << 16) | (g << 8) | b).toString(16).padStart(6, '0')}`
}

interface BasePlayerPanelProps {
  /** Player display name */
  name: string
  /** Panel color theme - can be a PlayerColor name or hex string */
  color: string
  /** Background image URL or data URI */
  backgroundImage?: string
  /** Whether the panel is inverted (rotated 180deg for face-to-face play) */
  inverted?: boolean
  /** Content for the header area (right side, next to name) */
  headerContent?: ReactNode
  /** Main content (life display, etc.) */
  children: ReactNode
  /** Footer content (counters, etc.) */
  footerContent?: ReactNode
  /** Click handler for the panel (used for expand/collapse in Commander) */
  onClick?: () => void
  /** Click handler for the player name (used for color picker) */
  onNameClick?: () => void
}

export function BasePlayerPanel({
  name,
  color,
  backgroundImage,
  inverted = false,
  headerContent,
  children,
  footerContent,
  onClick,
  onNameClick,
}: BasePlayerPanelProps) {
  const backgroundStyle = getBackgroundStyle(color, backgroundImage)

  const handleNameClick = (e: React.MouseEvent) => {
    if (onNameClick) {
      e.stopPropagation()
      onNameClick()
    }
  }

  return (
    <div
      className={`${styles.container} ${inverted ? styles.inverted : ''}`}
      style={backgroundStyle}
      onClick={onClick}
    >
      <div className={styles.header}>
        <span
          className={`${styles.playerName} ${onNameClick ? styles.clickable : ''}`}
          onClick={handleNameClick}
        >
          {name}
          {onNameClick && (
            <span className={styles.editHint}>tap to customize</span>
          )}
        </span>
        {headerContent && (
          <div className={styles.headerContent}>{headerContent}</div>
        )}
      </div>

      <div className={styles.main}>{children}</div>

      {footerContent && <div className={styles.footer}>{footerContent}</div>}
    </div>
  )
}
