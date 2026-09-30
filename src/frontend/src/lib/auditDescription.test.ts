import { describe, it, expect } from 'vitest'
import { describeAuditEntry } from './auditDescription'
import type { AuditLogEntry, EventSnapshotResponse } from '../api/types'
import { AuditActionType } from '../api/types'

const player = (id: string, name: string, seed: number) => ({
  id,
  name,
  seed,
  matchWins: 0,
  matchLosses: 0,
  byeReceived: false,
  isDropped: false,
})

const snapshot = {
  players: [player('a', 'Alex', 1), player('p', 'Priya', 2)],
  rounds: [
    {
      roundNumber: 2,
      status: 1,
      matches: [
        {
          id: 'm1',
          matchCode: 'r2-m0',
          playerAId: 'a',
          playerBId: 'p',
          winnerId: 'p',
          status: 2,
          isBye: false,
        },
      ],
    },
  ],
} as unknown as EventSnapshotResponse

const entry = (overrides: Partial<AuditLogEntry>): AuditLogEntry => ({
  id: 'x',
  actionType: AuditActionType.MatchFinalized,
  entityType: 'Match',
  entityId: 'm1',
  reason: null,
  createdAt: '2026-01-01T00:00:00Z',
  ...overrides,
})

describe('describeAuditEntry', () => {
  it('describes a match result using the winner recorded at the time', () => {
    // The snapshot now says Priya won (after a reopen); the entry recorded Alex
    expect(describeAuditEntry(entry({ winnerId: 'a' }), snapshot)).toBe(
      'Round 2: Alex def. Priya'
    )
  })

  it('describes a match without a result as a pairing', () => {
    expect(
      describeAuditEntry(
        entry({ actionType: AuditActionType.MatchReopened }),
        snapshot
      )
    ).toBe('Round 2: Alex vs Priya')
  })

  it('handles matches that no longer exist', () => {
    expect(describeAuditEntry(entry({ entityId: 'gone' }), snapshot)).toBe(
      'A match that was later re-paired'
    )
  })

  it('includes the round number for round actions', () => {
    expect(
      describeAuditEntry(
        entry({ entityType: 'Round', entityId: null, roundNumber: 3 }),
        snapshot
      )
    ).toBe('Round 3')
  })

  it('falls back to the recorded name for players who left', () => {
    expect(
      describeAuditEntry(
        entry({ entityType: 'Player', entityId: 'left', playerName: 'Sam' }),
        snapshot
      )
    ).toBe('Sam')
    expect(
      describeAuditEntry(
        entry({ entityType: 'Player', entityId: 'a' }),
        snapshot
      )
    ).toBe('Alex')
  })

  it('has no subject line for event-level actions', () => {
    expect(
      describeAuditEntry(entry({ entityType: 'Event', entityId: null }), null)
    ).toBe('')
  })
})
