/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useState, useCallback, useEffect, type ReactNode } from 'react';
import { storage, type HostSession, type PlayerSession } from '../lib/storage';

interface AuthState {
  hostSession: HostSession | null;
  playerSession: PlayerSession | null;
}

interface AuthContextValue extends AuthState {
  setHostSession: (session: HostSession) => void;
  setPlayerSession: (session: PlayerSession) => void;
  clearHostSession: () => void;
  clearPlayerSession: () => void;
  clearAll: () => void;
  isHost: (eventId: string) => boolean;
  getHostToken: (eventId: string) => string | null;
  getPlayerId: (eventId: string) => string | null;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => ({
    hostSession: storage.getHostSession(),
    playerSession: storage.getPlayerSession(),
  }));

  const setHostSession = useCallback((session: HostSession) => {
    storage.setHostSession(session);
    setState((prev) => ({ ...prev, hostSession: session }));
  }, []);

  const setPlayerSession = useCallback((session: PlayerSession) => {
    storage.setPlayerSession(session);
    setState((prev) => ({ ...prev, playerSession: session }));
  }, []);

  const clearHostSession = useCallback(() => {
    storage.clearHostSession();
    setState((prev) => ({ ...prev, hostSession: null }));
  }, []);

  const clearPlayerSession = useCallback(() => {
    storage.clearPlayerSession();
    setState((prev) => ({ ...prev, playerSession: null }));
  }, []);

  const clearAll = useCallback(() => {
    storage.clearAll();
    setState({ hostSession: null, playerSession: null });
  }, []);

  const isHost = useCallback(
    (eventId: string) => {
      return state.hostSession?.eventId === eventId;
    },
    [state.hostSession]
  );

  const getHostToken = useCallback(
    (eventId: string) => {
      if (state.hostSession?.eventId === eventId) {
        return state.hostSession.hostToken;
      }
      return null;
    },
    [state.hostSession]
  );

  const getPlayerId = useCallback(
    (eventId: string) => {
      if (state.playerSession?.eventId === eventId) {
        return state.playerSession.playerId;
      }
      return null;
    },
    [state.playerSession]
  );

  // Sync with localStorage changes from other tabs
  useEffect(() => {
    const handleStorageChange = () => {
      setState({
        hostSession: storage.getHostSession(),
        playerSession: storage.getPlayerSession(),
      });
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, []);

  return (
    <AuthContext.Provider
      value={{
        ...state,
        setHostSession,
        setPlayerSession,
        clearHostSession,
        clearPlayerSession,
        clearAll,
        isHost,
        getHostToken,
        getPlayerId,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
