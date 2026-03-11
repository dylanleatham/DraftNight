import { useCallback } from 'react'
import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { useEventErrorHandler } from './useEventErrorHandler'

interface UsePlayerActionsResult {
  leaveEvent: () => Promise<boolean>
}

export function usePlayerActions(eventId: string): UsePlayerActionsResult {
  const { getPlayerToken } = useAuth()
  const handleError = useEventErrorHandler()

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
