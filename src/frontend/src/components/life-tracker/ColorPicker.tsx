import { useState } from 'react'
import type { PlayerColor } from './BasePlayerPanel'
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
  onClose: () => void
}

export function ColorPicker({
  currentColor,
  onSelectColor,
  onSelectImage,
  currentImage,
  onClose,
}: ColorPickerProps) {
  const [customHex, setCustomHex] = useState('')
  const [imageUrl, setImageUrl] = useState(currentImage ?? '')
  const [activeTab, setActiveTab] = useState<'color' | 'image'>('color')

  const handlePresetSelect = (color: PlayerColor) => {
    onSelectColor(color)
    onClose()
  }

  const handleCustomHex = () => {
    const hex = customHex.trim()
    if (/^#?[0-9A-Fa-f]{6}$/.test(hex)) {
      const normalizedHex = hex.startsWith('#') ? hex : `#${hex}`
      onSelectColor(normalizedHex)
      onClose()
    }
  }

  const handleImageUrl = () => {
    if (onSelectImage) {
      onSelectImage(imageUrl.trim() || undefined)
      onClose()
    }
  }

  const handleClearImage = () => {
    if (onSelectImage) {
      onSelectImage(undefined)
      setImageUrl('')
    }
  }

  const handleFileUpload = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0]
    if (!file || !onSelectImage) return

    // Check file size (warn if > 500KB)
    if (file.size > 500 * 1024) {
      if (
        !confirm(
          'Image is larger than 500KB. Large images may slow down the app. Continue?'
        )
      ) {
        return
      }
    }

    const reader = new FileReader()
    reader.onload = (event) => {
      const dataUrl = event.target?.result as string
      onSelectImage(dataUrl)
      onClose()
    }
    reader.readAsDataURL(file)
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
              className={`${styles.tab} ${activeTab === 'color' ? styles.activeTab : ''}`}
              onClick={() => setActiveTab('color')}
            >
              Color
            </button>
            <button
              className={`${styles.tab} ${activeTab === 'image' ? styles.activeTab : ''}`}
              onClick={() => setActiveTab('image')}
            >
              Image
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

            <div className={styles.customSection}>
              <label className={styles.label}>Custom hex color</label>
              <div className={styles.customInput}>
                <span className={styles.hashPrefix}>#</span>
                <input
                  type="text"
                  className={styles.hexInput}
                  value={customHex.replace('#', '')}
                  onChange={(e) => setCustomHex(e.target.value)}
                  placeholder="ff5500"
                  maxLength={6}
                />
                <button
                  className={styles.applyButton}
                  onClick={handleCustomHex}
                >
                  Apply
                </button>
              </div>
            </div>
          </div>
        )}

        {activeTab === 'image' && onSelectImage && (
          <div className={styles.content}>
            <div className={styles.imageSection}>
              <label className={styles.label}>Image URL</label>
              <div className={styles.customInput}>
                <input
                  type="text"
                  className={styles.urlInput}
                  value={imageUrl}
                  onChange={(e) => setImageUrl(e.target.value)}
                  placeholder="https://..."
                />
                <button className={styles.applyButton} onClick={handleImageUrl}>
                  Apply
                </button>
              </div>

              <div className={styles.uploadSection}>
                <label className={styles.label}>Or upload image</label>
                <label className={styles.uploadButton}>
                  Choose File
                  <input
                    type="file"
                    accept="image/*"
                    onChange={handleFileUpload}
                    className={styles.fileInput}
                  />
                </label>
              </div>

              {currentImage && (
                <button
                  className={styles.clearButton}
                  onClick={handleClearImage}
                >
                  Clear Image
                </button>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}
