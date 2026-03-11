/* eslint-disable react-refresh/only-export-components */
import {
  createContext,
  useContext,
  useReducer,
  useCallback,
  useMemo,
  type ReactNode,
  type Dispatch,
} from 'react'
import type { EventSnapshotResponse, PlayerResponse } from '../api/types'
import { type ConnectionStatus } from '../hooks/useEventConnection'

interface EventState {
  snapshot: EventSnapshotResponse | null
  connectionStatus: ConnectionStatus
  error: string | null
  isLoading: boolean
}

type EventAction =
  | { type: 'SET_SNAPSHOT'; payload: EventSnapshotResponse }
  | { type: 'SET_CONNECTION_STATUS'; payload: ConnectionStatus }
  | { type: 'SET_ERROR'; payload: string | null }
  | { type: 'SET_LOADING'; payload: boolean }
  | { type: 'RESET' }

const initialState: EventState = {
  snapshot: null,
  connectionStatus: 'disconnected',
  error: null,
  isLoading: true,
}

function eventReducer(state: EventState, action: EventAction): EventState {
  switch (action.type) {
    case 'SET_SNAPSHOT':
      return {
        ...state,
        snapshot: action.payload,
        isLoading: false,
        error: null,
      }
    case 'SET_CONNECTION_STATUS':
      return { ...state, connectionStatus: action.payload }
    case 'SET_ERROR':
      return { ...state, error: action.payload, isLoading: false }
    case 'SET_LOADING':
      return { ...state, isLoading: action.payload }
    case 'RESET':
      return initialState
    default:
      return state
  }
}

interface EventContextValue {
  state: EventState
  dispatch: Dispatch<EventAction>
  getPlayer: (playerId: string) => PlayerResponse | undefined
}

const EventContext = createContext<EventContextValue | null>(null)

export function EventProvider({ children }: { children: ReactNode }) {
  const [state, dispatch] = useReducer(eventReducer, initialState)

  const getPlayer = useCallback(
    (playerId: string) => {
      return state.snapshot?.players.find((p) => p.id === playerId)
    },
    [state.snapshot]
  )

  const contextValue = useMemo(
    () => ({ state, dispatch, getPlayer }),
    [state, dispatch, getPlayer]
  )

  return (
    <EventContext.Provider value={contextValue}>
      {children}
    </EventContext.Provider>
  )
}

export function useEvent(): EventContextValue {
  const context = useContext(EventContext)
  if (!context) {
    throw new Error('useEvent must be used within an EventProvider')
  }
  return context
}

// Helper hook for common snapshot access patterns
export function useEventSnapshot(): EventSnapshotResponse | null {
  const { state } = useEvent()
  return state.snapshot
}

export function useConnectionStatus(): ConnectionStatus {
  const { state } = useEvent()
  return state.connectionStatus
}

export function useEventError(): string | null {
  const { state } = useEvent()
  return state.error
}

export function useIsEventLoading(): boolean {
  const { state } = useEvent()
  return state.isLoading
}
