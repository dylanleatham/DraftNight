import { useReducer, useEffect, useCallback } from 'react'
import type {
  DraftSession,
  DraftAction,
  DraftPlayer,
} from '../types/lifeTracker'
import { DRAFT_STARTING_LIFE, MAX_POISON } from '../types/lifeTracker'
import { lifeTrackerStorage } from '../lib/lifeTrackerStorage'

function generateId(): string {
  return Math.random().toString(36).substring(2, 9)
}

function createPlayer(
  id: string,
  name: string,
  startingLife: number
): DraftPlayer {
  return {
    id,
    name,
    life: startingLife,
    poison: 0,
    miscCounters: [],
  }
}

function createSession(
  sessionId: string,
  playerAName: string,
  playerBName: string,
  eventId?: string,
  matchId?: string
): DraftSession {
  const now = Date.now()
  return {
    id: sessionId,
    mode: 'draft',
    createdAt: now,
    updatedAt: now,
    startingLife: DRAFT_STARTING_LIFE,
    playerA: createPlayer('playerA', playerAName, DRAFT_STARTING_LIFE),
    playerB: createPlayer('playerB', playerBName, DRAFT_STARTING_LIFE),
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
  eventId?: string
  matchId?: string
}

export function useLifeTracker(options: UseLifeTrackerOptions) {
  const {
    sessionId,
    playerAName = 'Player 1',
    playerBName = 'Player 2',
    eventId,
    matchId,
  } = options

  // Try to load existing session or create new one
  const initialSession =
    lifeTrackerStorage.getDraftSession(sessionId) ??
    createSession(sessionId, playerAName, playerBName, eventId, matchId)

  const [session, dispatch] = useReducer(draftReducer, initialSession)

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
