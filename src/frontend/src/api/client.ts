import type {
  CreateEventRequest,
  CreateEventResponse,
  JoinEventRequest,
  JoinEventResponse,
  EventSnapshotResponse,
  MutationResponse,
  StandingsResponse,
  AuditLogResponse,
  ErrorResponse,
} from './types'

import { apiBaseUrl } from '../config'

const BASE_URL = `${apiBaseUrl}/api`

class ApiError extends Error {
  status: number
  code: string

  constructor(status: number, code: string, message: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
  }
}

async function handleResponse<T>(response: Response): Promise<T> {
  if (!response.ok) {
    const error: ErrorResponse = await response.json().catch(() => ({
      code: 'UNKNOWN_ERROR',
      message: response.statusText,
    }))
    throw new ApiError(response.status, error.code, error.message)
  }
  return response.json()
}

interface AuthTokens {
  hostToken?: string
  playerToken?: string
}

function buildHeaders(auth?: string | AuthTokens): HeadersInit {
  const headers: HeadersInit = {
    'Content-Type': 'application/json',
  }
  if (typeof auth === 'string') {
    // Legacy support: string is treated as host token
    headers['X-Host-Token'] = auth
  } else if (auth) {
    if (auth.hostToken) {
      headers['X-Host-Token'] = auth.hostToken
    }
    if (auth.playerToken) {
      headers['X-Player-Token'] = auth.playerToken
    }
  }
  return headers
}

export const api = {
  // Event Lifecycle
  async createEvent(request: CreateEventRequest): Promise<CreateEventResponse> {
    const response = await fetch(`${BASE_URL}/events`, {
      method: 'POST',
      headers: buildHeaders(),
      body: JSON.stringify(request),
    })
    return handleResponse<CreateEventResponse>(response)
  },

  async joinEvent(request: JoinEventRequest): Promise<JoinEventResponse> {
    const response = await fetch(`${BASE_URL}/events/join`, {
      method: 'POST',
      headers: buildHeaders(),
      body: JSON.stringify(request),
    })
    return handleResponse<JoinEventResponse>(response)
  },

  async getEvent(eventId: string): Promise<EventSnapshotResponse> {
    const response = await fetch(`${BASE_URL}/events/${eventId}`, {
      headers: buildHeaders(),
    })
    return handleResponse<EventSnapshotResponse>(response)
  },

  // Host Control
  async startEvent(
    eventId: string,
    hostToken: string,
    expectedVersion: number
  ): Promise<MutationResponse> {
    const response = await fetch(`${BASE_URL}/events/${eventId}/start`, {
      method: 'POST',
      headers: buildHeaders(hostToken),
      body: JSON.stringify({ expectedVersion }),
    })
    return handleResponse<MutationResponse>(response)
  },

  async publishPairings(
    eventId: string,
    roundNumber: number,
    hostToken: string,
    expectedVersion: number
  ): Promise<MutationResponse> {
    const response = await fetch(
      `${BASE_URL}/events/${eventId}/rounds/${roundNumber}/publish`,
      {
        method: 'POST',
        headers: buildHeaders(hostToken),
        body: JSON.stringify({ expectedVersion }),
      }
    )
    return handleResponse<MutationResponse>(response)
  },

  async finalizeMatch(
    eventId: string,
    matchId: string,
    auth: AuthTokens,
    winnerId: string,
    expectedVersion: number
  ): Promise<MutationResponse> {
    const response = await fetch(
      `${BASE_URL}/events/${eventId}/matches/${matchId}/finalize`,
      {
        method: 'POST',
        headers: buildHeaders(auth),
        body: JSON.stringify({ winnerId, expectedVersion }),
      }
    )
    return handleResponse<MutationResponse>(response)
  },

  async dropPlayer(
    eventId: string,
    playerId: string,
    hostToken: string,
    expectedVersion: number,
    reason?: string
  ): Promise<MutationResponse> {
    const response = await fetch(
      `${BASE_URL}/events/${eventId}/players/${playerId}/drop`,
      {
        method: 'POST',
        headers: buildHeaders(hostToken),
        body: JSON.stringify({ expectedVersion, reason }),
      }
    )
    return handleResponse<MutationResponse>(response)
  },

  async allocatePrizes(
    eventId: string,
    hostToken: string,
    expectedVersion: number
  ): Promise<MutationResponse> {
    const response = await fetch(
      `${BASE_URL}/events/${eventId}/prizes/allocate`,
      {
        method: 'POST',
        headers: buildHeaders(hostToken),
        body: JSON.stringify({ expectedVersion }),
      }
    )
    return handleResponse<MutationResponse>(response)
  },

  async cancelEvent(
    eventId: string,
    hostToken: string,
    expectedVersion: number
  ): Promise<MutationResponse> {
    const response = await fetch(`${BASE_URL}/events/${eventId}/cancel`, {
      method: 'POST',
      headers: buildHeaders(hostToken),
      body: JSON.stringify({ expectedVersion }),
    })
    return handleResponse<MutationResponse>(response)
  },

  // Host Repair
  async reopenMatch(
    eventId: string,
    matchId: string,
    hostToken: string,
    expectedVersion: number,
    reason: string
  ): Promise<MutationResponse> {
    const response = await fetch(
      `${BASE_URL}/events/${eventId}/matches/${matchId}/reopen`,
      {
        method: 'POST',
        headers: buildHeaders(hostToken),
        body: JSON.stringify({ expectedVersion, reason }),
      }
    )
    return handleResponse<MutationResponse>(response)
  },

  // Read-Only
  async getStandings(eventId: string): Promise<StandingsResponse> {
    const response = await fetch(`${BASE_URL}/events/${eventId}/standings`, {
      headers: buildHeaders(),
    })
    return handleResponse<StandingsResponse>(response)
  },

  async getAuditLog(
    eventId: string,
    hostToken: string
  ): Promise<AuditLogResponse> {
    const response = await fetch(`${BASE_URL}/events/${eventId}/audit`, {
      headers: buildHeaders(hostToken),
    })
    return handleResponse<AuditLogResponse>(response)
  },

  async leaveEvent(
    eventId: string,
    playerToken: string,
    expectedVersion: number
  ): Promise<MutationResponse> {
    const response = await fetch(`${BASE_URL}/events/${eventId}/leave`, {
      method: 'POST',
      headers: buildHeaders({ playerToken }),
      body: JSON.stringify({ expectedVersion }),
    })
    return handleResponse<MutationResponse>(response)
  },
}

export { ApiError }
