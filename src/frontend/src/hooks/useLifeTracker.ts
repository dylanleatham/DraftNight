import { useReducer, useEffect, useCallback } from 'react'
import type {
  DraftSession,
  DraftAction,
  DraftPlayer,
} from '../types/lifeTracker'
import { DRAFT_STARTING_LIFE, MAX_POISON } from '../types/lifeTracker'
import { lifeTrackerStorage } from '../lib/lifeTrackerStorage'
import type { Archetype } from '../lib/archetypeImages'
import { getRandomArchetypes, getRandomImage } from '../lib/archetypeImages'
import { archetypeStorage } from '../lib/archetypeStorage'

function generateId(): string {
  return Math.random().toString(36).substring(2, 9)
}

interface CreatePlayerOptions {
  archetype?: Archetype
  backgroundImage?: string
}

function createPlayer(
  id: string,
  name: string,
  startingLife: number,
  options: CreatePlayerOptions = {}
): DraftPlayer {
  return {
    id,
    name,
    life: startingLife,
    poison: 0,
    miscCounters: [],
    archetype: options.archetype,
    backgroundImage: options.backgroundImage,
  }
}

interface CreateSessionOptions {
  archetypeA?: Archetype
  archetypeB?: Archetype
  imageA?: string
  imageB?: string
}

function createSession(
  sessionId: string,
  playerAName: string,
  playerBName: string,
  startingLife: number,
  eventId?: string,
  matchId?: string,
  options: CreateSessionOptions = {}
): DraftSession {
  const now = Date.now()
  return {
    id: sessionId,
    mode: 'draft',
    createdAt: now,
    updatedAt: now,
    startingLife,
    playerA: createPlayer('playerA', playerAName, startingLife, {
      archetype: options.archetypeA,
      backgroundImage: options.imageA,
    }),
    playerB: createPlayer('playerB', playerBName, startingLife, {
      archetype: options.archetypeB,
      backgroundImage: options.imageB,
    }),
    gameWins: {
      playerA: 0,
      playerB: 0,
    },
    eventId,
    matchId,
  }
}

function draftReducer(state: DraftSession, action: DraftAction): DraftSession {
  switch (action.type) {
    case 'SET_LIFE': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      return {
        ...state,
        [player]: { ...state[player], life: action.life },
      }
    }

    case 'ADJUST_LIFE': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      return {
        ...state,
        [player]: {
          ...state[player],
          life: state[player].life + action.delta,
        },
      }
    }

    case 'SET_POISON': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      const poison = Math.max(0, Math.min(MAX_POISON, action.poison))
      return {
        ...state,
        [player]: { ...state[player], poison },
      }
    }

    case 'ADJUST_POISON': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      const newPoison = Math.max(
        0,
        Math.min(MAX_POISON, state[player].poison + action.delta)
      )
      return {
        ...state,
        [player]: { ...state[player], poison: newPoison },
      }
    }

    case 'TOGGLE_GAME_WIN': {
      const current = state.gameWins[action.player]
      const newWins = current >= 2 ? 0 : current + 1
      const isRecordingWin = newWins > current

      // When recording a win (not wrapping back to 0), reset life and poison
      // for the next game
      if (isRecordingWin) {
        return {
          ...state,
          playerA: {
            ...state.playerA,
            life: state.startingLife,
            poison: 0,
          },
          playerB: {
            ...state.playerB,
            life: state.startingLife,
            poison: 0,
          },
          gameWins: {
            ...state.gameWins,
            [action.player]: newWins,
          },
        }
      }

      // Wrapping back to 0 (correction) - only update game wins
      return {
        ...state,
        gameWins: {
          ...state.gameWins,
          [action.player]: newWins,
        },
      }
    }

    case 'RESET_GAME': {
      // Reset life and poison but keep game wins and misc counters (sticky)
      // If a match was won (someone has 2+ wins), decrement their wins by 1
      // to clear the match-winning state
      const matchWinner =
        state.gameWins.playerA >= 2
          ? 'playerA'
          : state.gameWins.playerB >= 2
            ? 'playerB'
            : null
      const newGameWins = matchWinner
        ? {
            ...state.gameWins,
            [matchWinner]: state.gameWins[matchWinner] - 1,
          }
        : state.gameWins

      return {
        ...state,
        playerA: {
          ...state.playerA,
          life: state.startingLife,
          poison: 0,
          // miscCounters, panelColor, backgroundImage preserved
        },
        playerB: {
          ...state.playerB,
          life: state.startingLife,
          poison: 0,
          // miscCounters, panelColor, backgroundImage preserved
        },
        gameWins: newGameWins,
      }
    }

    case 'RESET_MATCH': {
      // Reset life, poison, and game wins but keep misc counters (sticky)
      return {
        ...state,
        playerA: {
          ...state.playerA,
          life: state.startingLife,
          poison: 0,
          // miscCounters, panelColor, backgroundImage preserved
        },
        playerB: {
          ...state.playerB,
          life: state.startingLife,
          poison: 0,
          // miscCounters, panelColor, backgroundImage preserved
        },
        gameWins: {
          playerA: 0,
          playerB: 0,
        },
      }
    }

    case 'SET_PLAYER_NAME': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      return {
        ...state,
        [player]: { ...state[player], name: action.name },
      }
    }

    case 'ADD_MISC_COUNTER': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      return {
        ...state,
        [player]: {
          ...state[player],
          miscCounters: [
            ...state[player].miscCounters,
            { id: generateId(), name: action.name, value: 0 },
          ],
        },
      }
    }

    case 'REMOVE_MISC_COUNTER': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      return {
        ...state,
        [player]: {
          ...state[player],
          miscCounters: state[player].miscCounters.filter(
            (c) => c.id !== action.counterId
          ),
        },
      }
    }

    case 'SET_MISC_COUNTER': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      return {
        ...state,
        [player]: {
          ...state[player],
          miscCounters: state[player].miscCounters.map((c) =>
            c.id === action.counterId ? { ...c, value: action.value } : c
          ),
        },
      }
    }

    case 'ADJUST_MISC_COUNTER': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      return {
        ...state,
        [player]: {
          ...state[player],
          miscCounters: state[player].miscCounters.map((c) =>
            c.id === action.counterId
              ? { ...c, value: c.value + action.delta }
              : c
          ),
        },
      }
    }

    case 'SET_PANEL_COLOR': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      return {
        ...state,
        [player]: { ...state[player], panelColor: action.color },
      }
    }

    case 'SET_BACKGROUND_IMAGE': {
      const player = action.playerId === 'playerA' ? 'playerA' : 'playerB'
      return {
        ...state,
        [player]: { ...state[player], backgroundImage: action.imageUrl },
      }
    }

    case 'LOAD_SESSION':
      return action.session

    default:
      return state
  }
}

interface UseLifeTrackerOptions {
  sessionId: string
  playerAName?: string
  playerBName?: string
  startingLife?: number
  eventId?: string
  matchId?: string
  // Archetype assignments (for standalone mode, passed via URL params)
  archetypeA?: Archetype
  archetypeB?: Archetype
  // Event player IDs (for draft event mode, to look up archetypes)
  playerAId?: string
  playerBId?: string
}

function getInitialSession(options: UseLifeTrackerOptions): DraftSession {
  const {
    sessionId,
    playerAName = 'Player 1',
    playerBName = 'Player 2',
    startingLife = DRAFT_STARTING_LIFE,
    eventId,
    matchId,
    archetypeA,
    archetypeB,
    playerAId,
    playerBId,
  } = options

  // Try to load existing session
  const existingSession = lifeTrackerStorage.getDraftSession(sessionId)
  if (existingSession) {
    // For event sessions, we may need to assign a new image for a new game
    // The session already has archetypes, but we want a fresh image each game
    if (
      eventId &&
      existingSession.playerA.archetype &&
      existingSession.playerB.archetype
    ) {
      // Get next images for this game (different from previous games in the event)
      const imageA = archetypeStorage.getNextImage(
        eventId,
        playerAId || 'playerA'
      )
      const imageB = archetypeStorage.getNextImage(
        eventId,
        playerBId || 'playerB'
      )

      if (imageA && !existingSession.playerA.backgroundImage) {
        existingSession.playerA.backgroundImage = imageA
      }
      if (imageB && !existingSession.playerB.backgroundImage) {
        existingSession.playerB.backgroundImage = imageB
      }
    }
    return existingSession
  }

  // Create new session with archetype images
  let finalArchetypeA = archetypeA
  let finalArchetypeB = archetypeB
  let imageA: string | undefined
  let imageB: string | undefined

  if (eventId && playerAId && playerBId) {
    // Draft event mode: initialize or get archetypes from storage
    const eventData = archetypeStorage.getEventData(eventId)
    if (eventData) {
      // Event already has archetypes assigned
      finalArchetypeA = eventData.playerArchetypes[playerAId]
      finalArchetypeB = eventData.playerArchetypes[playerBId]
    }
    // Get images for this game
    imageA = archetypeStorage.getNextImage(eventId, playerAId)
    imageB = archetypeStorage.getNextImage(eventId, playerBId)
  } else if (archetypeA && archetypeB) {
    // Standalone mode with archetypes passed via URL params
    imageA = getRandomImage(archetypeA)
    imageB = getRandomImage(archetypeB)
  } else if (!eventId) {
    // Standalone mode without archetypes - assign random ones
    const [randomA, randomB] = getRandomArchetypes(2)
    finalArchetypeA = randomA
    finalArchetypeB = randomB
    imageA = getRandomImage(randomA)
    imageB = getRandomImage(randomB)
  }

  return createSession(
    sessionId,
    playerAName,
    playerBName,
    startingLife,
    eventId,
    matchId,
    {
      archetypeA: finalArchetypeA,
      archetypeB: finalArchetypeB,
      imageA,
      imageB,
    }
  )
}

export function useLifeTracker(options: UseLifeTrackerOptions) {
  const [session, dispatch] = useReducer(
    draftReducer,
    options,
    getInitialSession
  )

  // Auto-save on every state change
  useEffect(() => {
    lifeTrackerStorage.saveDraftSession(session)
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

  const toggleGameWin = useCallback((player: 'playerA' | 'playerB') => {
    dispatch({ type: 'TOGGLE_GAME_WIN', player })
  }, [])

  const resetGame = useCallback(() => {
    dispatch({ type: 'RESET_GAME' })
  }, [])

  const resetMatch = useCallback(() => {
    dispatch({ type: 'RESET_MATCH' })
  }, [])

  const setPlayerName = useCallback((playerId: string, name: string) => {
    dispatch({ type: 'SET_PLAYER_NAME', playerId, name })
  }, [])

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
    playerA: session.playerA,
    playerB: session.playerB,
    gameWins: session.gameWins,
    adjustLife,
    setLife,
    adjustPoison,
    setPoison,
    toggleGameWin,
    resetGame,
    resetMatch,
    setPlayerName,
    addMiscCounter,
    removeMiscCounter,
    adjustMiscCounter,
    setMiscCounter,
    setPanelColor,
    setBackgroundImage,
  }
}
