import { useCallback } from 'react'
import { api, ApiError } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { useEvent } from '../context/EventContext'

interface UsePlayerActionsResult {
  leaveEvent: () => Promise<boolean>
}

export function usePlayerActions(eventId: string): UsePlayerActionsResult {
  const { getPlayerToken } = useAuth()
  const { dispatch } = useEvent()

  const handleError = useCallback(
    (err: unknown) => {
      if (err instanceof ApiError) {
        dispatch({ type: 'SET_ERROR', payload: err.message })
      } else if (err instanceof Error) {
        dispatch({ type: 'SET_ERROR', payload: err.message })
      } else {
        dispatch({ type: 'SET_ERROR', payload: 'An unexpected error occurred' })
      }
    },
    [dispatch]
  )

  const leaveEvent = useCallback(async () => {
    try {
      const playerToken = getPlayerToken(eventId)
      if (!playerToken) {
        throw new Error('Not authorized as player')
      }
      // Fetch latest snapshot to get current version
      const latestSnapshot = await api.getEvent(eventId)
      const result = await api.leaveEvent(
        eventId,
        playerToken,
        latestSnapshot.version
      )
      return result.success
    } catch (err) {
      console.error('Leave event error', err)
      handleError(err)
      return false
    }
  }, [eventId, getPlayerToken, handleError])

  return {
    leaveEvent,
  }
}
