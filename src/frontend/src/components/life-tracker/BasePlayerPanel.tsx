import type { ReactNode, CSSProperties } from 'react'
import {
  COLOR_GRADIENTS,
  isPlayerColor,
  adjustBrightness,
} from './panelColors'
export type { PlayerColor } from './panelColors'
import styles from './BasePlayerPanel.module.css'

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
