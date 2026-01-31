import type { CSSProperties } from 'react'
import styles from './CharacterPortrait.module.css'
import type { Character } from '../../data/characters'

export type PortraitSize = 'small' | 'medium' | 'large'

interface CharacterPortraitProps {
  character: Character
  size?: PortraitSize
  showName?: boolean
  className?: string
}

const sizeMap: Record<PortraitSize, number> = {
  small: 48,
  medium: 64,
  large: 96,
}

export function CharacterPortrait({
  character,
  size = 'medium',
  showName = false,
  className = '',
}: CharacterPortraitProps) {
  const pixelSize = sizeMap[size]

  const containerStyle: CSSProperties = {
    '--portrait-bg': character.primaryColor,
    '--portrait-border': character.accentColor,
    '--portrait-size': `${pixelSize}px`,
  } as CSSProperties

  return (
    <div
      className={`${styles.container} ${styles[size]} ${className}`}
      style={containerStyle}
    >
      <div className={styles.portrait}>
        <span className={styles.emoji} role="img" aria-label={character.name}>
          {character.emoji}
        </span>
      </div>
      {showName && <span className={styles.name}>{character.name}</span>}
    </div>
  )
}
