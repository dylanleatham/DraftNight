import { useState } from 'react'
import type { PlayerColor } from './BasePlayerPanel'
import type { Archetype } from '../../lib/archetypeImages'
import { archetypeImages } from '../../lib/archetypeImages'
import styles from './ColorPicker.module.css'

const PRESET_COLORS: { name: PlayerColor; hex: string; label: string }[] = [
  { name: 'blue', hex: '#3b82f6', label: 'Blue' },
  { name: 'red', hex: '#ef4444', label: 'Red' },
  { name: 'green', hex: '#22c55e', label: 'Green' },
  { name: 'purple', hex: '#a855f7', label: 'Purple' },
  { name: 'orange', hex: '#f97316', label: 'Orange' },
  { name: 'teal', hex: '#14b8a6', label: 'Teal' },
]

interface ColorPickerProps {
  currentColor: string
  onSelectColor: (color: string) => void
  onSelectImage?: (imageUrl: string | undefined) => void
  currentImage?: string
  archetype?: Archetype
  onClose: () => void
}

export function ColorPicker({
  currentColor,
  onSelectColor,
  onSelectImage,
  currentImage,
  archetype,
  onClose,
}: ColorPickerProps) {
  const [activeTab, setActiveTab] = useState<'color' | 'image'>('image')

  // Get available images for the player's archetype
  const availableImages = archetype ? archetypeImages.get(archetype) || [] : []

  const handlePresetSelect = (color: PlayerColor) => {
    onSelectColor(color)
    // Clear background image so color shows
    if (onSelectImage) {
      onSelectImage(undefined)
    }
    onClose()
  }

  const handleImageSelect = (imageUrl: string) => {
    if (onSelectImage) {
      onSelectImage(imageUrl)
      onClose()
    }
  }

  return (
    <div className={styles.overlay} onClick={onClose}>
      <div className={styles.modal} onClick={(e) => e.stopPropagation()}>
        <div className={styles.header}>
          <h3 className={styles.title}>Customize Panel</h3>
          <button className={styles.closeButton} onClick={onClose}>
            ×
          </button>
        </div>

        {onSelectImage && (
          <div className={styles.tabs}>
            <button
              className={`${styles.tab} ${activeTab === 'image' ? styles.activeTab : ''}`}
              onClick={() => setActiveTab('image')}
            >
              Image
            </button>
            <button
              className={`${styles.tab} ${activeTab === 'color' ? styles.activeTab : ''}`}
              onClick={() => setActiveTab('color')}
            >
              Color
            </button>
          </div>
        )}

        {activeTab === 'color' && (
          <div className={styles.content}>
            <div className={styles.swatches}>
              {PRESET_COLORS.map((color) => (
                <button
                  key={color.name}
                  className={`${styles.swatch} ${currentColor === color.name ? styles.selected : ''}`}
                  style={{ background: color.hex }}
                  onClick={() => handlePresetSelect(color.name)}
                  aria-label={color.label}
                />
              ))}
            </div>
          </div>
        )}

        {activeTab === 'image' && onSelectImage && (
          <div className={styles.content}>
            {availableImages.length > 0 ? (
              <div className={styles.imageGrid}>
                {availableImages.map((imageUrl, index) => (
                  <button
                    key={index}
                    className={`${styles.imageOption} ${currentImage === imageUrl ? styles.selectedImage : ''}`}
                    onClick={() => handleImageSelect(imageUrl)}
                  >
                    <img src={imageUrl} alt={`${archetype} portrait ${index + 1}`} />
                  </button>
                ))}
              </div>
            ) : (
              <p className={styles.noImages}>No portraits available</p>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
