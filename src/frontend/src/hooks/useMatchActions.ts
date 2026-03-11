import { useCallback } from 'react'
import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { useEventErrorHandler } from './useEventErrorHandler'
import type { MatchResponse } from '../api/types'

interface UseMatchActionsResult {
  finalizeMatch: (
    matchId: string,
    winnerId: string,
    match: MatchResponse
  ) => Promise<boolean>
}

/**
 * Hook for match actions that can be performed by either the host or players in the match.
 * Prefers host token if available, otherwise falls back to player token for their own matches.
 */
export function useMatchActions(eventId: string): UseMatchActionsResult {
  const { getHostToken, getPlayerToken, getPlayerId } = useAuth()
  const handleError = useEventErrorHandler()

  const finalizeMatch = useCallback(
    async (matchId: string, winnerId: string, match: MatchResponse) => {
      try {
        const hostToken = getHostToken(eventId)
        const playerToken = getPlayerToken(eventId)
        const playerId = getPlayerId(eventId)

        // Check if user is authorized
        const isHost = !!hostToken
        const isPlayerInMatch =
          playerId === match.playerAId || playerId === match.playerBId

        if (!isHost && !isPlayerInMatch) {
          throw new Error('Not authorized to finalize this match')
        }

        // Use host token if available, otherwise use player token
        const auth = hostToken
          ? { hostToken }
          : playerToken
            ? { playerToken }
            : null

        if (!auth) {
          throw new Error('No valid authentication token')
        }

        const latestSnapshot = await api.getEvent(eventId)
        const result = await api.finalizeMatch(
          eventId,
          matchId,
          auth,
          winnerId,
          latestSnapshot.version
        )
        return result.success
      } catch (err) {
        handleError(err)
        return false
      }
    },
    [eventId, getHostToken, getPlayerToken, getPlayerId, handleError]
  )

  return {
    finalizeMatch,
  }
}
