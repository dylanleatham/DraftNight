import type {
  DraftSession,
  CommanderSession,
  LifeTrackerSession,
} from '../types/lifeTracker';
import { MAX_SESSIONS_PER_MODE } from '../types/lifeTracker';

const STORAGE_KEYS = {
  DRAFT_SESSIONS: 'draftapp_life_tracker_draft',
  COMMANDER_SESSIONS: 'draftapp_life_tracker_commander',
} as const;

function parseJSON<T>(data: string | null, fallback: T): T {
  if (!data) return fallback;
  try {
    return JSON.parse(data) as T;
  } catch {
    return fallback;
  }
}

function pruneOldSessions<T extends LifeTrackerSession>(
  sessions: T[],
  maxCount: number
): T[] {
  if (sessions.length <= maxCount) return sessions;
  // Sort by updatedAt descending, keep most recent
  return sessions
    .sort((a, b) => b.updatedAt - a.updatedAt)
    .slice(0, maxCount);
}

export const lifeTrackerStorage = {
  // Draft Sessions
  getDraftSessions(): DraftSession[] {
    const data = localStorage.getItem(STORAGE_KEYS.DRAFT_SESSIONS);
    return parseJSON<DraftSession[]>(data, []);
  },

  getDraftSession(sessionId: string): DraftSession | null {
    const sessions = this.getDraftSessions();
    return sessions.find((s) => s.id === sessionId) ?? null;
  },

  saveDraftSession(session: DraftSession): void {
    const sessions = this.getDraftSessions();
    const index = sessions.findIndex((s) => s.id === session.id);
    const updatedSession = { ...session, updatedAt: Date.now() };

    if (index >= 0) {
      sessions[index] = updatedSession;
    } else {
      sessions.push(updatedSession);
    }

    const pruned = pruneOldSessions(sessions, MAX_SESSIONS_PER_MODE);
    localStorage.setItem(STORAGE_KEYS.DRAFT_SESSIONS, JSON.stringify(pruned));
  },

  deleteDraftSession(sessionId: string): void {
    const sessions = this.getDraftSessions();
    const filtered = sessions.filter((s) => s.id !== sessionId);
    localStorage.setItem(STORAGE_KEYS.DRAFT_SESSIONS, JSON.stringify(filtered));
  },

  // Commander Sessions
  getCommanderSessions(): CommanderSession[] {
    const data = localStorage.getItem(STORAGE_KEYS.COMMANDER_SESSIONS);
    return parseJSON<CommanderSession[]>(data, []);
  },

  getCommanderSession(sessionId: string): CommanderSession | null {
    const sessions = this.getCommanderSessions();
    return sessions.find((s) => s.id === sessionId) ?? null;
  },

  saveCommanderSession(session: CommanderSession): void {
    const sessions = this.getCommanderSessions();
    const index = sessions.findIndex((s) => s.id === session.id);
    const updatedSession = { ...session, updatedAt: Date.now() };

    if (index >= 0) {
      sessions[index] = updatedSession;
    } else {
      sessions.push(updatedSession);
    }

    const pruned = pruneOldSessions(sessions, MAX_SESSIONS_PER_MODE);
    localStorage.setItem(STORAGE_KEYS.COMMANDER_SESSIONS, JSON.stringify(pruned));
  },

  deleteCommanderSession(sessionId: string): void {
    const sessions = this.getCommanderSessions();
    const filtered = sessions.filter((s) => s.id !== sessionId);
    localStorage.setItem(STORAGE_KEYS.COMMANDER_SESSIONS, JSON.stringify(filtered));
  },

  // Recent sessions (for resume functionality)
  getRecentSessions(): LifeTrackerSession[] {
    const draftSessions = this.getDraftSessions();
    const commanderSessions = this.getCommanderSessions();

    return [...draftSessions, ...commanderSessions]
      .sort((a, b) => b.updatedAt - a.updatedAt)
      .slice(0, 5);
  },

  // Clear all sessions
  clearAll(): void {
    localStorage.removeItem(STORAGE_KEYS.DRAFT_SESSIONS);
    localStorage.removeItem(STORAGE_KEYS.COMMANDER_SESSIONS);
  },
};
