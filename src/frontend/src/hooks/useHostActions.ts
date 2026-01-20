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
  const { dispatch } = useEvent();

  const handleError = useCallback(
    (err: unknown) => {
      if (err instanceof ApiError) {
        dispatch({ type: 'SET_ERROR', payload: err.message });
      } else if (err instanceof Error) {
        dispatch({ type: 'SET_ERROR', payload: err.message });
      } else {
        dispatch({ type: 'SET_ERROR', payload: 'An unexpected error occurred' });
      }
    },
    [dispatch]
  );

  const startEvent = useCallback(async () => {
    try {
      const hostToken = getHostToken(eventId);
      if (!hostToken) {
        throw new Error('Not authorized as host');
      }
      // Fetch latest snapshot to get current version (workaround for SignalR sync issues)
      const latestSnapshot = await api.getEvent(eventId);
      const result = await api.startEvent(eventId, hostToken, latestSnapshot.version);
      return result.success;
    } catch (err) {
      console.error('Start event error', err);
      handleError(err);
      return false;
    }
  }, [eventId, getHostToken, handleError]);

  const publishPairings = useCallback(
    async (roundNumber: number) => {
      try {
        const hostToken = getHostToken(eventId);
        if (!hostToken) throw new Error('Not authorized as host');
        const latestSnapshot = await api.getEvent(eventId);
        const result = await api.publishPairings(eventId, roundNumber, hostToken, latestSnapshot.version);
        return result.success;
      } catch (err) {
        handleError(err);
        return false;
      }
    },
    [eventId, getHostToken, handleError]
  );

  const finalizeMatch = useCallback(
    async (matchId: string, winnerId: string) => {
      try {
        const hostToken = getHostToken(eventId);
        if (!hostToken) throw new Error('Not authorized as host');
        const latestSnapshot = await api.getEvent(eventId);
        const result = await api.finalizeMatch(eventId, matchId, hostToken, winnerId, latestSnapshot.version);
        return result.success;
      } catch (err) {
        handleError(err);
        return false;
      }
    },
    [eventId, getHostToken, handleError]
  );

  const dropPlayer = useCallback(
    async (playerId: string, reason?: string) => {
      try {
        const hostToken = getHostToken(eventId);
        if (!hostToken) throw new Error('Not authorized as host');
        const latestSnapshot = await api.getEvent(eventId);
        const result = await api.dropPlayer(eventId, playerId, hostToken, latestSnapshot.version, reason);
        return result.success;
      } catch (err) {
        handleError(err);
        return false;
      }
    },
    [eventId, getHostToken, handleError]
  );

  const allocatePrizes = useCallback(async () => {
    try {
      const hostToken = getHostToken(eventId);
      if (!hostToken) throw new Error('Not authorized as host');
      const latestSnapshot = await api.getEvent(eventId);
      const result = await api.allocatePrizes(eventId, hostToken, latestSnapshot.version);
      return result.success;
    } catch (err) {
      handleError(err);
      return false;
    }
  }, [eventId, getHostToken, handleError]);

  const reopenMatch = useCallback(
    async (matchId: string, reason: string) => {
      try {
        const hostToken = getHostToken(eventId);
        if (!hostToken) throw new Error('Not authorized as host');
        const latestSnapshot = await api.getEvent(eventId);
        const result = await api.reopenMatch(eventId, matchId, hostToken, latestSnapshot.version, reason);
        return result.success;
      } catch (err) {
        handleError(err);
        return false;
      }
    },
    [eventId, getHostToken, handleError]
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
