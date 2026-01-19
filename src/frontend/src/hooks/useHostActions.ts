import { useCallback } from 'react';
import { api, ApiError } from '../api/client';
import { useAuth } from '../context/AuthContext';
import { useEvent } from '../context/EventContext';

interface UseHostActionsResult {
  startEvent: () => Promise<boolean>;
  publishPairings: (roundNumber: number) => Promise<boolean>;
  finalizeMatch: (matchId: string, winnerId: string) => Promise<boolean>;
  dropPlayer: (playerId: string, reason?: string) => Promise<boolean>;
  allocatePrizes: () => Promise<boolean>;
  reopenMatch: (matchId: string, reason: string) => Promise<boolean>;
}

export function useHostActions(eventId: string): UseHostActionsResult {
  const { getHostToken } = useAuth();
  const { state, dispatch } = useEvent();

  const getTokenAndVersion = useCallback(() => {
    const hostToken = getHostToken(eventId);
    if (!hostToken) {
      throw new Error('Not authorized as host');
    }
    const version = state.snapshot?.version;
    if (version === undefined) {
      throw new Error('No event snapshot available');
    }
    return { hostToken, version };
  }, [eventId, getHostToken, state.snapshot]);

  const handleError = useCallback(
    (err: unknown) => {
      if (err instanceof ApiError) {
        dispatch({ type: 'SET_ERROR', payload: err.message });
        if (err.code === 'VERSION_CONFLICT') {
          // SignalR will send updated snapshot, just log for now
          console.warn('Version conflict - waiting for fresh snapshot');
        }
      } else {
        dispatch({ type: 'SET_ERROR', payload: 'An unexpected error occurred' });
      }
    },
    [dispatch]
  );

  const startEvent = useCallback(async () => {
    try {
      const { hostToken, version } = getTokenAndVersion();
      const result = await api.startEvent(eventId, hostToken, version);
      return result.success;
    } catch (err) {
      handleError(err);
      return false;
    }
  }, [eventId, getTokenAndVersion, handleError]);

  const publishPairings = useCallback(
    async (roundNumber: number) => {
      try {
        const { hostToken, version } = getTokenAndVersion();
        const result = await api.publishPairings(eventId, roundNumber, hostToken, version);
        return result.success;
      } catch (err) {
        handleError(err);
        return false;
      }
    },
    [eventId, getTokenAndVersion, handleError]
  );

  const finalizeMatch = useCallback(
    async (matchId: string, winnerId: string) => {
      try {
        const { hostToken, version } = getTokenAndVersion();
        const result = await api.finalizeMatch(eventId, matchId, hostToken, winnerId, version);
        return result.success;
      } catch (err) {
        handleError(err);
        return false;
      }
    },
    [eventId, getTokenAndVersion, handleError]
  );

  const dropPlayer = useCallback(
    async (playerId: string, reason?: string) => {
      try {
        const { hostToken, version } = getTokenAndVersion();
        const result = await api.dropPlayer(eventId, playerId, hostToken, version, reason);
        return result.success;
      } catch (err) {
        handleError(err);
        return false;
      }
    },
    [eventId, getTokenAndVersion, handleError]
  );

  const allocatePrizes = useCallback(async () => {
    try {
      const { hostToken, version } = getTokenAndVersion();
      const result = await api.allocatePrizes(eventId, hostToken, version);
      return result.success;
    } catch (err) {
      handleError(err);
      return false;
    }
  }, [eventId, getTokenAndVersion, handleError]);

  const reopenMatch = useCallback(
    async (matchId: string, reason: string) => {
      try {
        const { hostToken, version } = getTokenAndVersion();
        const result = await api.reopenMatch(eventId, matchId, hostToken, version, reason);
        return result.success;
      } catch (err) {
        handleError(err);
        return false;
      }
    },
    [eventId, getTokenAndVersion, handleError]
  );

  return {
    startEvent,
    publishPairings,
    finalizeMatch,
    dropPlayer,
    allocatePrizes,
    reopenMatch,
  };
}
