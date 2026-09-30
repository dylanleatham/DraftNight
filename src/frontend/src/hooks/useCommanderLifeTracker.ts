import { useReducer, useEffect, useCallback } from 'react'
import type {
  CommanderSession,
  CommanderAction,
  CommanderPlayer,
} from '../types/lifeTracker'
import { COMMANDER_STARTING_LIFE, MAX_POISON } from '../types/lifeTracker'
import { lifeTrackerStorage } from '../lib/lifeTrackerStorage'
import type { Archetype } from '../lib/archetypeImages'
import { getRandomArchetypes, getRandomImage } from '../lib/archetypeImages'

function generateId(): string {
  return Math.random().toString(36).substring(2, 9)
}

interface CreatePlayerOptions {
  archetype?: Archetype
  backgroundImage?: string
}

function createPlayer(
  index: number,
  name: string,
  allPlayerIds: string[],
  startingLife: number,
  options: CreatePlayerOptions = {}
): CommanderPlayer {
  const id = `player_${index}`
  return {
    id,
    name,
    life: startingLife,
    poison: 0,
    commanderDamage: allPlayerIds
      .filter((pid) => pid !== id)
      .map((pid) => ({ fromPlayerId: pid, amount: 0 })),
    miscCounters: [],
    archetype: options.archetype,
    backgroundImage: options.backgroundImage,
  }
}

interface PlayerArchetypeConfig {
  archetype: Archetype
  image: string | undefined
}

function createSession(
  sessionId: string,
  playerNames: string[],
  startingLife: number,
  archetypeConfigs?: PlayerArchetypeConfig[]
): CommanderSession {
  const now = Date.now()
  const playerIds = playerNames.map((_, i) => `player_${i}`)

  return {
    id: sessionId,
    mode: 'commander',
    createdAt: now,
    updatedAt: now,
    startingLife,
    players: playerNames.map((name, i) =>
      createPlayer(i, name, playerIds, startingLife, {
        archetype: archetypeConfigs?.[i]?.archetype,
        backgroundImage: archetypeConfigs?.[i]?.image,
      })
    ),
  }
}

function commanderReducer(
  state: CommanderSession,
  action: CommanderAction
): CommanderSession {
  switch (action.type) {
    case 'SET_LIFE': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId ? { ...p, life: action.life } : p
        ),
      }
    }

    case 'ADJUST_LIFE': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId ? { ...p, life: p.life + action.delta } : p
        ),
      }
    }

    case 'SET_POISON': {
      const poison = Math.max(0, Math.min(MAX_POISON, action.poison))
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId ? { ...p, poison } : p
        ),
      }
    }

    case 'ADJUST_POISON': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId
            ? {
                ...p,
                poison: Math.max(
                  0,
                  Math.min(MAX_POISON, p.poison + action.delta)
                ),
              }
            : p
        ),
      }
    }

    case 'SET_COMMANDER_DAMAGE': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId
            ? {
                ...p,
                commanderDamage: p.commanderDamage.map((cd) =>
                  cd.fromPlayerId === action.fromPlayerId
                    ? { ...cd, amount: Math.max(0, action.amount) }
                    : cd
                ),
              }
            : p
        ),
      }
    }

    case 'ADJUST_COMMANDER_DAMAGE': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId
            ? {
                ...p,
                commanderDamage: p.commanderDamage.map((cd) =>
                  cd.fromPlayerId === action.fromPlayerId
                    ? { ...cd, amount: Math.max(0, cd.amount + action.delta) }
                    : cd
                ),
              }
            : p
        ),
      }
    }

    case 'ADD_MISC_COUNTER': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId
            ? {
                ...p,
                miscCounters: [
                  ...p.miscCounters,
                  { id: generateId(), name: action.name, value: 0 },
                ],
              }
            : p
        ),
      }
    }

    case 'REMOVE_MISC_COUNTER': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId
            ? {
                ...p,
                miscCounters: p.miscCounters.filter(
                  (c) => c.id !== action.counterId
                ),
              }
            : p
        ),
      }
    }

    case 'SET_MISC_COUNTER': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId
            ? {
                ...p,
                miscCounters: p.miscCounters.map((c) =>
                  c.id === action.counterId ? { ...c, value: action.value } : c
                ),
              }
            : p
        ),
      }
    }

    case 'ADJUST_MISC_COUNTER': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId
            ? {
                ...p,
                miscCounters: p.miscCounters.map((c) =>
                  c.id === action.counterId
                    ? { ...c, value: c.value + action.delta }
                    : c
                ),
              }
            : p
        ),
      }
    }

    case 'SET_PLAYER_NAME': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId ? { ...p, name: action.name } : p
        ),
      }
    }

    case 'SET_PANEL_COLOR': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId ? { ...p, panelColor: action.color } : p
        ),
      }
    }

    case 'SET_BACKGROUND_IMAGE': {
      return {
        ...state,
        players: state.players.map((p) =>
          p.id === action.playerId
            ? { ...p, backgroundImage: action.imageUrl }
            : p
        ),
      }
    }

    case 'RESET_ALL': {
      const playerIds = state.players.map((p) => p.id)
      return {
        ...state,
        players: state.players.map((p) => ({
          ...p,
          life: state.startingLife,
          poison: 0,
          commanderDamage: playerIds
            .filter((pid) => pid !== p.id)
            .map((pid) => ({ fromPlayerId: pid, amount: 0 })),
          // miscCounters, panelColor, backgroundImage preserved (sticky)
        })),
      }
    }

    case 'LOAD_SESSION':
      return action.session

    default:
      return state
  }
}

interface UseCommanderLifeTrackerOptions {
  sessionId: string
  playerNames?: string[]
  startingLife?: number
  // Archetype assignments (passed via URL params from setup page)
  archetypes?: Archetype[]
}

function getInitialCommanderSession(
  options: UseCommanderLifeTrackerOptions
): CommanderSession {
  const {
    sessionId,
    playerNames = ['Player 1', 'Player 2', 'Player 3', 'Player 4'],
    startingLife = COMMANDER_STARTING_LIFE,
    archetypes,
  } = options

  // Try to load existing session
  const existingSession = lifeTrackerStorage.getCommanderSession(sessionId)
  if (existingSession) {
    return existingSession
  }

  // Create new session with archetype images
  let archetypeConfigs: PlayerArchetypeConfig[] | undefined

  if (archetypes && archetypes.length === playerNames.length) {
    // Archetypes passed via URL params
    archetypeConfigs = archetypes.map((archetype) => ({
      archetype,
      image: getRandomImage(archetype),
    }))
  } else {
    // Standalone mode - assign random archetypes
    const randomArchetypes = getRandomArchetypes(playerNames.length)
    archetypeConfigs = randomArchetypes.map((archetype) => ({
      archetype,
      image: getRandomImage(archetype),
    }))
  }

  return createSession(sessionId, playerNames, startingLife, archetypeConfigs)
}

export function useCommanderLifeTracker(
  options: UseCommanderLifeTrackerOptions
) {
  const [session, dispatch] = useReducer(
    commanderReducer,
    options,
    getInitialCommanderSession
  )

  // Auto-save on every state change
  useEffect(() => {
    lifeTrackerStorage.saveCommanderSession(session)
  }, [session])

  // Action helpers
  const adjustLife = useCallback((playerId: string, delta: number) => {
    dispatch({ type: 'ADJUST_LIFE', playerId, delta })
  }, [])

  const setLife = useCallback((playerId: string, life: number) => {
    dispatch({ type: 'SET_LIFE', playerId, life })
  }, [])

  const adjustPoison = useCallback((playerId: string, delta: number) => {
    dispatch({ type: 'ADJUST_POISON', playerId, delta })
  }, [])

  const setPoison = useCallback((playerId: string, poison: number) => {
    dispatch({ type: 'SET_POISON', playerId, poison })
  }, [])

  const adjustCommanderDamage = useCallback(
    (playerId: string, fromPlayerId: string, delta: number) => {
      dispatch({
        type: 'ADJUST_COMMANDER_DAMAGE',
        playerId,
        fromPlayerId,
        delta,
      })
    },
    []
  )

  const setCommanderDamage = useCallback(
    (playerId: string, fromPlayerId: string, amount: number) => {
      dispatch({ type: 'SET_COMMANDER_DAMAGE', playerId, fromPlayerId, amount })
    },
    []
  )

  const addMiscCounter = useCallback((playerId: string, name: string) => {
    dispatch({ type: 'ADD_MISC_COUNTER', playerId, name })
  }, [])

  const removeMiscCounter = useCallback(
    (playerId: string, counterId: string) => {
      dispatch({ type: 'REMOVE_MISC_COUNTER', playerId, counterId })
    },
    []
  )

  const adjustMiscCounter = useCallback(
    (playerId: string, counterId: string, delta: number) => {
      dispatch({ type: 'ADJUST_MISC_COUNTER', playerId, counterId, delta })
    },
    []
  )

  const setMiscCounter = useCallback(
    (playerId: string, counterId: string, value: number) => {
      dispatch({ type: 'SET_MISC_COUNTER', playerId, counterId, value })
    },
    []
  )

  const setPlayerName = useCallback((playerId: string, name: string) => {
    dispatch({ type: 'SET_PLAYER_NAME', playerId, name })
  }, [])

  const resetAll = useCallback(() => {
    dispatch({ type: 'RESET_ALL' })
  }, [])

  const setPanelColor = useCallback((playerId: string, color: string) => {
    dispatch({ type: 'SET_PANEL_COLOR', playerId, color })
  }, [])

  const setBackgroundImage = useCallback(
    (playerId: string, imageUrl: string | undefined) => {
      dispatch({ type: 'SET_BACKGROUND_IMAGE', playerId, imageUrl })
    },
    []
  )

  return {
    session,
    players: session.players,
    adjustLife,
    setLife,
    adjustPoison,
    setPoison,
    adjustCommanderDamage,
    setCommanderDamage,
    addMiscCounter,
    removeMiscCounter,
    adjustMiscCounter,
    setMiscCounter,
    setPlayerName,
    resetAll,
    setPanelColor,
    setBackgroundImage,
  }
}
