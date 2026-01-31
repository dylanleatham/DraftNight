// Status constants matching backend enums
export const EventStatus = {
  Setup: 0,
  Active: 1,
  Completed: 2,
  Archived: 3,
} as const
export type EventStatus = (typeof EventStatus)[keyof typeof EventStatus]

export const RoundStatus = {
  Pending: 0,
  PairingsPublished: 1,
  Closed: 2,
} as const
export type RoundStatus = (typeof RoundStatus)[keyof typeof RoundStatus]

export const MatchStatus = {
  NotStarted: 0,
  InProgress: 1,
  Final: 2,
} as const
export type MatchStatus = (typeof MatchStatus)[keyof typeof MatchStatus]

export const TournamentFormat = {
  RoundRobin: 0,
  Swiss: 1,
} as const
export type TournamentFormat =
  (typeof TournamentFormat)[keyof typeof TournamentFormat]

// Response types
export interface PlayerResponse {
  id: string
  name: string
  seed: number
  matchWins: number
  matchLosses: number
  byeReceived: boolean
  isDropped: boolean
}

export interface MatchResponse {
  id: string
  matchCode: string
  playerAId: string
  playerBId: string | null
  winnerId: string | null
  status: MatchStatus
  isBye: boolean
}

export interface RoundResponse {
  roundNumber: number
  status: RoundStatus
  matches: MatchResponse[]
}

export interface PrizeAllocationResponse {
  playerId: string
  packsAwarded: number
}

export interface EventSnapshotResponse {
  id: string
  name: string
  status: EventStatus
  joinCode: string | null
  packsInBox: number
  prizePacks: number
  format: TournamentFormat
  totalRounds: number
  currentRound: number
  prizesAllocated: boolean
  version: number
  players: PlayerResponse[]
  rounds: RoundResponse[]
  prizeAllocations: PrizeAllocationResponse[]
}

export interface CreateEventResponse {
  eventId: string
  joinCode: string
  hostToken: string
  playerId: string
  playerToken: string
}

export interface JoinEventResponse {
  eventId: string
  playerId: string
  playerToken: string
}

export interface MutationResponse {
  success: boolean
  newVersion: number
  error?: string
}

export interface StandingEntry {
  rank: number
  playerId: string
  playerName: string
  matchWins: number
  matchLosses: number
  byeReceived: boolean
  isDropped: boolean
}

export interface StandingsResponse {
  standings: StandingEntry[]
}

export interface ErrorResponse {
  code: string
  message: string
  details?: unknown
}

// Request types
export interface CreateEventRequest {
  name: string
  packsInBox: number
  hostPin: string
  hostName: string
}

export interface JoinEventRequest {
  joinCode: string
  playerName: string
  playerPin: string
}

export interface HostActionRequest {
  expectedVersion: number
  reason?: string
}

export interface FinalizeMatchRequest {
  winnerId: string
  expectedVersion: number
}

export interface ReopenMatchRequest {
  expectedVersion: number
  reason: string
}

export interface DropPlayerRequest {
  expectedVersion: number
  reason?: string
}

// Audit Log types
export const AuditActionType = {
  EventCreated: 0,
  EventStarted: 1,
  PlayerJoined: 2,
  PlayerDropped: 3,
  PairingsGenerated: 4,
  MatchFinalized: 5,
  PrizesAllocated: 6,
  HostRepair: 7,
  MatchReopened: 8,
  OpponentsSwapped: 9,
  RoundReopened: 10,
  PairingsRegenerated: 11,
  EventCancelled: 12,
} as const
export type AuditActionType =
  (typeof AuditActionType)[keyof typeof AuditActionType]

export interface AuditLogEntry {
  id: string
  actionType: AuditActionType
  entityType: string
  entityId: string | null
  reason: string | null
  createdAt: string
}

export interface AuditLogResponse {
  entries: AuditLogEntry[]
}
