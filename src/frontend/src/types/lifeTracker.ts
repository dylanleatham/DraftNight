// Life Tracker Type Definitions

export type TrackerMode = 'draft' | 'commander';

// Draft Mode Types (1v1)
export interface DraftPlayer {
  id: string;
  name: string;
  life: number;
  poison: number;
  miscCounters: MiscCounter[];
  panelColor?: string;  // PlayerColor name or hex string
  backgroundImage?: string;  // URL or data URI
}

export interface DraftSession {
  id: string;
  mode: 'draft';
  createdAt: number;
  updatedAt: number;
  startingLife: number;
  playerA: DraftPlayer;
  playerB: DraftPlayer;
  gameWins: {
    playerA: number;
    playerB: number;
  };
  // Optional link to event match
  eventId?: string;
  matchId?: string;
}

// Commander Mode Types (2-6 players)
export interface CommanderDamage {
  fromPlayerId: string;
  amount: number;
}

export interface MiscCounter {
  id: string;
  name: string;
  value: number;
}

export interface CommanderPlayer {
  id: string;
  name: string;
  life: number;
  poison: number;
  commanderDamage: CommanderDamage[];
  miscCounters: MiscCounter[];
  panelColor?: string;  // PlayerColor name or hex string
  backgroundImage?: string;  // URL or data URI
}

export interface CommanderSession {
  id: string;
  mode: 'commander';
  createdAt: number;
  updatedAt: number;
  startingLife: number;
  players: CommanderPlayer[];
}

export type LifeTrackerSession = DraftSession | CommanderSession;

// Reducer Actions for Draft Mode
export type DraftAction =
  | { type: 'SET_LIFE'; playerId: string; life: number }
  | { type: 'ADJUST_LIFE'; playerId: string; delta: number }
  | { type: 'SET_POISON'; playerId: string; poison: number }
  | { type: 'ADJUST_POISON'; playerId: string; delta: number }
  | { type: 'TOGGLE_GAME_WIN'; player: 'playerA' | 'playerB' }
  | { type: 'RESET_GAME' }
  | { type: 'RESET_MATCH' }
  | { type: 'SET_PLAYER_NAME'; playerId: string; name: string }
  | { type: 'ADD_MISC_COUNTER'; playerId: string; name: string }
  | { type: 'REMOVE_MISC_COUNTER'; playerId: string; counterId: string }
  | { type: 'SET_MISC_COUNTER'; playerId: string; counterId: string; value: number }
  | { type: 'ADJUST_MISC_COUNTER'; playerId: string; counterId: string; delta: number }
  | { type: 'SET_PANEL_COLOR'; playerId: string; color: string }
  | { type: 'SET_BACKGROUND_IMAGE'; playerId: string; imageUrl: string | undefined }
  | { type: 'LOAD_SESSION'; session: DraftSession };

// Reducer Actions for Commander Mode
export type CommanderAction =
  | { type: 'SET_LIFE'; playerId: string; life: number }
  | { type: 'ADJUST_LIFE'; playerId: string; delta: number }
  | { type: 'SET_POISON'; playerId: string; poison: number }
  | { type: 'ADJUST_POISON'; playerId: string; delta: number }
  | { type: 'SET_COMMANDER_DAMAGE'; playerId: string; fromPlayerId: string; amount: number }
  | { type: 'ADJUST_COMMANDER_DAMAGE'; playerId: string; fromPlayerId: string; delta: number }
  | { type: 'ADD_MISC_COUNTER'; playerId: string; name: string }
  | { type: 'REMOVE_MISC_COUNTER'; playerId: string; counterId: string }
  | { type: 'SET_MISC_COUNTER'; playerId: string; counterId: string; value: number }
  | { type: 'ADJUST_MISC_COUNTER'; playerId: string; counterId: string; delta: number }
  | { type: 'SET_PLAYER_NAME'; playerId: string; name: string }
  | { type: 'SET_PANEL_COLOR'; playerId: string; color: string }
  | { type: 'SET_BACKGROUND_IMAGE'; playerId: string; imageUrl: string | undefined }
  | { type: 'RESET_ALL' }
  | { type: 'LOAD_SESSION'; session: CommanderSession };

// Constants
export const DRAFT_STARTING_LIFE = 20;
export const COMMANDER_STARTING_LIFE = 40;
export const MAX_POISON = 10;
export const COMMANDER_DAMAGE_LETHAL = 21;
export const MAX_SESSIONS_PER_MODE = 10;
