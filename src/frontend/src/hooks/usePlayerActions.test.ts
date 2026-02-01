import { renderHook } from '@testing-library/react'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { usePlayerActions } from './usePlayerActions'

// Mock dependencies
vi.mock('../api/client', () => ({
  api: {
    getEvent: vi.fn(),
    leaveEvent: vi.fn(),
  },
  ApiError: class ApiError extends Error {
    status: number
    code: string
    constructor(status: number, code: string, message: string) {
      super(message)
      this.status = status
      this.code = code
    }
  },
}))

vi.mock('../context/AuthContext', () => ({
  useAuth: vi.fn(() => ({
    getPlayerToken: vi.fn(() => 'test-player-token'),
  })),
}))

vi.mock('../context/EventContext', () => ({
  useEvent: vi.fn(() => ({
    dispatch: vi.fn(),
  })),
}))

import { api } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { useEvent } from '../context/EventContext'

describe('usePlayerActions', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('returns leaveEvent function', () => {
    const { result } = renderHook(() => usePlayerActions('test-event-id'))

    expect(result.current.leaveEvent).toBeDefined()
    expect(typeof result.current.leaveEvent).toBe('function')
  })

  it('leaveEvent returns false when player token is not available', async () => {
    vi.mocked(useAuth).mockReturnValue({
      getPlayerToken: vi.fn(() => null),
    } as unknown as ReturnType<typeof useAuth>)

    const { result } = renderHook(() => usePlayerActions('test-event-id'))

    const success = await result.current.leaveEvent()

    expect(success).toBe(false)
    expect(api.getEvent).not.toHaveBeenCalled()
    expect(api.leaveEvent).not.toHaveBeenCalled()
  })

  it('leaveEvent calls API with correct parameters', async () => {
    const mockGetEvent = vi.mocked(api.getEvent)
    const mockLeaveEvent = vi.mocked(api.leaveEvent)
    const mockGetPlayerToken = vi.fn(() => 'test-player-token')

    vi.mocked(useAuth).mockReturnValue({
      getPlayerToken: mockGetPlayerToken,
    } as unknown as ReturnType<typeof useAuth>)

    mockGetEvent.mockResolvedValue({
      id: 'test-event-id',
      version: 5,
      name: 'Test Event',
      status: 0,
      joinCode: 'ABC123',
      packsInBox: 36,
      prizePacks: 24,
      format: 0,
      totalRounds: 3,
      currentRound: 1,
      prizesAllocated: false,
      players: [],
      rounds: [],
      prizeAllocations: [],
    })

    mockLeaveEvent.mockResolvedValue({
      success: true,
      newVersion: 6,
    })

    const { result } = renderHook(() => usePlayerActions('test-event-id'))

    const success = await result.current.leaveEvent()

    expect(success).toBe(true)
    expect(mockGetEvent).toHaveBeenCalledWith('test-event-id')
    expect(mockLeaveEvent).toHaveBeenCalledWith(
      'test-event-id',
      'test-player-token',
      5
    )
  })

  it('leaveEvent returns false and dispatches error when API fails', async () => {
    const mockGetEvent = vi.mocked(api.getEvent)
    const mockDispatch = vi.fn()

    vi.mocked(useAuth).mockReturnValue({
      getPlayerToken: vi.fn(() => 'test-player-token'),
    } as unknown as ReturnType<typeof useAuth>)

    vi.mocked(useEvent).mockReturnValue({
      dispatch: mockDispatch,
    } as unknown as ReturnType<typeof useEvent>)

    mockGetEvent.mockRejectedValue(new Error('Network error'))

    const { result } = renderHook(() => usePlayerActions('test-event-id'))

    const success = await result.current.leaveEvent()

    expect(success).toBe(false)
    expect(mockDispatch).toHaveBeenCalledWith({
      type: 'SET_ERROR',
      payload: 'Network error',
    })
  })
})
