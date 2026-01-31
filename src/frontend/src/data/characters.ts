/**
 * Character Archetypes for Draft Night
 * 8 distinct player archetypes with themed colors
 */

export interface Character {
  id: string
  name: string
  emoji: string
  primaryColor: string
  accentColor: string
  description: string
}

export const characters: Character[] = [
  {
    id: 'shark',
    name: 'The Shark',
    emoji: '🦈',
    primaryColor: '#1e40af',
    accentColor: '#60a5fa',
    description: 'Ruthless competitor who plays to win',
  },
  {
    id: 'ruleslawyer',
    name: 'The Rules Lawyer',
    emoji: '🧙',
    primaryColor: '#4c1d95',
    accentColor: '#a78bfa',
    description: 'Rules lawyer with encyclopedic knowledge',
  },
  {
    id: 'streamer',
    name: 'The Streamer',
    emoji: '📺',
    primaryColor: '#be185d',
    accentColor: '#f472b6',
    description: 'Playing for the content and the clips',
  },
  {
    id: 'animefan',
    name: 'The Anime Fan',
    emoji: '🎌',
    primaryColor: '#dc2626',
    accentColor: '#f87171',
    description: 'Here for the anime art and flavor',
  },
  {
    id: 'strategist',
    name: 'The Strategist',
    emoji: '♟️',
    primaryColor: '#0f766e',
    accentColor: '#2dd4bf',
    description: 'Calculated moves, optimal plays',
  },
  {
    id: 'finalboss',
    name: 'Final Boss',
    emoji: '👑',
    primaryColor: '#b45309',
    accentColor: '#fbbf24',
    description: 'The one everyone wants to beat',
  },
  {
    id: 'cosplayer',
    name: 'The Cosplayer',
    emoji: '🎭',
    primaryColor: '#7e22ce',
    accentColor: '#c084fc',
    description: 'Committed to the aesthetic above all',
  },
  {
    id: 'wildcard',
    name: 'The Wildcard',
    emoji: '🃏',
    primaryColor: '#15803d',
    accentColor: '#4ade80',
    description: 'Unpredictable chaos agent',
  },
]

/**
 * Get a character by ID
 */
export function getCharacter(id: string): Character | undefined {
  return characters.find((c) => c.id === id)
}

/**
 * Get a character by index (wraps around if needed)
 */
export function getCharacterByIndex(index: number): Character {
  return characters[index % characters.length]
}

/**
 * Get a random character
 */
export function getRandomCharacter(): Character {
  return characters[Math.floor(Math.random() * characters.length)]
}
