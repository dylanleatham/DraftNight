export type PlayerColor =
  | 'blue'
  | 'red'
  | 'green'
  | 'purple'
  | 'orange'
  | 'teal'

export const PLAYER_COLORS: PlayerColor[] = [
  'blue',
  'red',
  'green',
  'purple',
  'orange',
  'teal',
]

export const COLOR_GRADIENTS: Record<PlayerColor, string> = {
  blue: 'linear-gradient(135deg, #3b82f6 0%, #1d4ed8 100%)',
  red: 'linear-gradient(135deg, #ef4444 0%, #b91c1c 100%)',
  green: 'linear-gradient(135deg, #22c55e 0%, #15803d 100%)',
  purple: 'linear-gradient(135deg, #a855f7 0%, #7e22ce 100%)',
  orange: 'linear-gradient(135deg, #f97316 0%, #c2410c 100%)',
  teal: 'linear-gradient(135deg, #14b8a6 0%, #0f766e 100%)',
}

export function isPlayerColor(color: string): color is PlayerColor {
  return PLAYER_COLORS.includes(color as PlayerColor)
}

export function adjustBrightness(hex: string, percent: number): string {
  const cleanHex = hex.startsWith('#') ? hex.slice(1) : hex
  const num = parseInt(cleanHex, 16)
  const r = Math.max(0, Math.min(255, (num >> 16) + percent))
  const g = Math.max(0, Math.min(255, ((num >> 8) & 0x00ff) + percent))
  const b = Math.max(0, Math.min(255, (num & 0x0000ff) + percent))
  return `#${((r << 16) | (g << 8) | b).toString(16).padStart(6, '0')}`
}
