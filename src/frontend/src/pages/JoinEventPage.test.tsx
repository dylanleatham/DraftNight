import { render, screen, fireEvent, waitFor } from '@testing-library/react'
import { MemoryRouter, Routes, Route } from 'react-router-dom'
import { describe, it, expect, vi, beforeEach } from 'vitest'
import { JoinEventPage } from './JoinEventPage'

vi.mock('../api/client', () => ({
  api: {
    joinEvent: vi.fn(),
    resumeSession: vi.fn(),
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

const setPlayerSession = vi.fn()
const setHostSession = vi.fn()
vi.mock('../context/AuthContext', () => ({
  useAuth: () => ({ setPlayerSession, setHostSession }),
}))

import { api, ApiError } from '../api/client'

function renderPage(initialUrl = '/join') {
  render(
    <MemoryRouter initialEntries={[initialUrl]}>
      <Routes>
        <Route path="/join" element={<JoinEventPage />} />
        <Route path="/event/:eventId/*" element={<p>event page</p>} />
      </Routes>
    </MemoryRouter>
  )
}

function fillForm(code: string, name: string, pin: string) {
  fireEvent.change(screen.getByLabelText('Join Code'), {
    target: { value: code },
  })
  fireEvent.change(screen.getByLabelText('Your Name'), {
    target: { value: name },
  })
  fireEvent.change(screen.getByLabelText('Your PIN'), {
    target: { value: pin },
  })
}

describe('JoinEventPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  it('joins as a new player by default', async () => {
    vi.mocked(api.joinEvent).mockResolvedValue({
      eventId: 'e1',
      playerId: 'p1',
      playerToken: 'pt',
    })
    renderPage('/join?code=abc123')

    fillForm('ABC123', 'Sam', '0000')
    fireEvent.click(screen.getByRole('button', { name: 'Join Event' }))

    await screen.findByText('event page')
    expect(api.joinEvent).toHaveBeenCalledWith({
      joinCode: 'ABC123',
      playerName: 'Sam',
      playerPin: '0000',
    })
    expect(api.resumeSession).not.toHaveBeenCalled()
  })

  it('rejoin mode resumes the seat and restores the host session for the host', async () => {
    vi.mocked(api.resumeSession).mockResolvedValue({
      eventId: 'e1',
      joinCode: 'ABC123',
      playerId: 'p1',
      playerToken: 'new-pt',
      hostToken: 'new-ht',
    })
    renderPage()

    fireEvent.click(
      screen.getByRole('button', { name: /already joined on another device/i })
    )
    fillForm('abc123', ' Alex ', '1234')
    fireEvent.click(screen.getByRole('button', { name: 'Rejoin Event' }))

    await screen.findByText('event page')
    expect(api.resumeSession).toHaveBeenCalledWith({
      joinCode: 'ABC123',
      playerName: 'Alex',
      pin: '1234',
    })
    expect(setPlayerSession).toHaveBeenCalledWith({
      eventId: 'e1',
      playerId: 'p1',
      playerToken: 'new-pt',
    })
    expect(setHostSession).toHaveBeenCalledWith({
      eventId: 'e1',
      hostToken: 'new-ht',
      joinCode: 'ABC123',
    })
  })

  it('rejoin as a regular player does not set a host session', async () => {
    vi.mocked(api.resumeSession).mockResolvedValue({
      eventId: 'e1',
      joinCode: 'ABC123',
      playerId: 'p2',
      playerToken: 'new-pt',
      hostToken: null,
    })
    renderPage('/join?mode=rejoin')

    fillForm('ABC123', 'Sam', '0000')
    fireEvent.click(screen.getByRole('button', { name: 'Rejoin Event' }))

    await screen.findByText('event page')
    expect(setPlayerSession).toHaveBeenCalled()
    expect(setHostSession).not.toHaveBeenCalled()
  })

  it('shows a friendly message when rate limited', async () => {
    vi.mocked(api.resumeSession).mockRejectedValue(
      new ApiError(429, 'UNKNOWN_ERROR', 'Too Many Requests')
    )
    renderPage('/join?mode=rejoin')

    fillForm('ABC123', 'Sam', '0000')
    fireEvent.click(screen.getByRole('button', { name: 'Rejoin Event' }))

    await waitFor(() =>
      expect(screen.getByText(/too many attempts/i)).toBeInTheDocument()
    )
    expect(setPlayerSession).not.toHaveBeenCalled()
  })
})
