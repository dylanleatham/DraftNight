const STORAGE_KEYS = {
  HOST_SESSION: 'draftapp_host_session',
  PLAYER_SESSION: 'draftapp_player_session',
} as const

export interface HostSession {
  eventId: string
  hostToken: string
  joinCode: string
}

export interface PlayerSession {
  eventId: string
  playerId: string
  playerToken: string
}

export const storage = {
  // Host session
  getHostSession(): HostSession | null {
    try {
      const data = localStorage.getItem(STORAGE_KEYS.HOST_SESSION)
      return data ? JSON.parse(data) : null
    } catch {
      return null
    }
  },

  setHostSession(session: HostSession): void {
    localStorage.setItem(STORAGE_KEYS.HOST_SESSION, JSON.stringify(session))
  },

  clearHostSession(): void {
    localStorage.removeItem(STORAGE_KEYS.HOST_SESSION)
  },

  // Player session
  getPlayerSession(): PlayerSession | null {
    try {
      const data = localStorage.getItem(STORAGE_KEYS.PLAYER_SESSION)
      return data ? JSON.parse(data) : null
    } catch {
      return null
    }
  },

  setPlayerSession(session: PlayerSession): void {
    localStorage.setItem(STORAGE_KEYS.PLAYER_SESSION, JSON.stringify(session))
  },

  clearPlayerSession(): void {
    localStorage.removeItem(STORAGE_KEYS.PLAYER_SESSION)
  },

  // Clear all sessions
  clearAll(): void {
    localStorage.removeItem(STORAGE_KEYS.HOST_SESSION)
    localStorage.removeItem(STORAGE_KEYS.PLAYER_SESSION)
  },
}
