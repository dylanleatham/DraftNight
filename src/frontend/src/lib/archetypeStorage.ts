// Persistence for draft event archetype assignments

import type { Archetype } from './archetypeImages'
import { getRandomArchetypes, getRandomImage } from './archetypeImages'

interface DraftArchetypeData {
  playerArchetypes: Record<string, Archetype> // playerId -> archetype
  usedImages: Record<string, string[]> // playerId -> used image URLs
}

const STORAGE_KEY_PREFIX = 'draftapp_archetypes_'

function getStorageKey(eventId: string): string {
  return `${STORAGE_KEY_PREFIX}${eventId}`
}

export const archetypeStorage = {
  /**
   * Get existing archetype data for an event
   */
  getEventData(eventId: string): DraftArchetypeData | null {
    try {
      const key = getStorageKey(eventId)
      const data = localStorage.getItem(key)
      if (!data) return null
      return JSON.parse(data) as DraftArchetypeData
    } catch {
      return null
    }
  },

  /**
   * Initialize archetypes for a new event, assigning unique archetypes to each player
   */
  initializeEvent(eventId: string, playerIds: string[]): DraftArchetypeData {
    // Check if already initialized
    const existing = this.getEventData(eventId)
    if (existing) return existing

    // Get random unique archetypes for each player
    const archetypes = getRandomArchetypes(playerIds.length)

    const playerArchetypes: Record<string, Archetype> = {}
    const usedImages: Record<string, string[]> = {}

    playerIds.forEach((playerId, index) => {
      playerArchetypes[playerId] = archetypes[index]
      usedImages[playerId] = []
    })

    const data: DraftArchetypeData = {
      playerArchetypes,
      usedImages,
    }

    try {
      const key = getStorageKey(eventId)
      localStorage.setItem(key, JSON.stringify(data))
    } catch {
      // Storage full or unavailable - continue without persistence
    }

    return data
  },

  /**
   * Get the next image for a player in an event, tracking usage to avoid repeats
   */
  getNextImage(eventId: string, playerId: string): string | undefined {
    const data = this.getEventData(eventId)
    if (!data) return undefined

    const archetype = data.playerArchetypes[playerId]
    if (!archetype) return undefined

    const usedImages = data.usedImages[playerId] || []
    const image = getRandomImage(archetype, usedImages)

    if (image) {
      // Track this image as used
      data.usedImages[playerId] = [...usedImages, image]

      try {
        const key = getStorageKey(eventId)
        localStorage.setItem(key, JSON.stringify(data))
      } catch {
        // Storage full - continue without updating
      }
    }

    return image
  },

  /**
   * Get the archetype assigned to a player in an event
   */
  getPlayerArchetype(eventId: string, playerId: string): Archetype | undefined {
    const data = this.getEventData(eventId)
    if (!data) return undefined
    return data.playerArchetypes[playerId]
  },

  /**
   * Clear archetype data for an event
   */
  clearEvent(eventId: string): void {
    try {
      const key = getStorageKey(eventId)
      localStorage.removeItem(key)
    } catch {
      // Ignore errors
    }
  },
}
