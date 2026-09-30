import type { AuditLogEntry, EventSnapshotResponse } from '../api/types'

/**
 * Human-readable subject line for an audit entry, e.g. "Round 2: Alex def. Priya".
 * Uses the current snapshot to resolve names, falling back to details recorded with
 * the entry (players who left the lobby are no longer in the snapshot).
 */
export function describeAuditEntry(
  entry: AuditLogEntry,
  snapshot: EventSnapshotResponse | null
): string {
  const nameOf = (playerId: string | null | undefined) =>
    snapshot?.players.find((p) => p.id === playerId)?.name ?? 'Unknown player'

  switch (entry.entityType) {
    case 'Event':
      return ''

    case 'Player': {
      const current = snapshot?.players.find((p) => p.id === entry.entityId)
      return current?.name ?? entry.playerName ?? 'Unknown player'
    }

    case 'Round':
      return entry.roundNumber != null ? `Round ${entry.roundNumber}` : 'Round'

    case 'Match': {
      const round = snapshot?.rounds.find((r) =>
        r.matches.some((m) => m.id === entry.entityId)
      )
      const match = round?.matches.find((m) => m.id === entry.entityId)
      if (!round || !match) {
        // Regenerating pairings replaces matches, so older entries can outlive them
        return 'A match that was later re-paired'
      }

      if (entry.winnerId) {
        const loserId =
          entry.winnerId === match.playerAId ? match.playerBId : match.playerAId
        return `Round ${round.roundNumber}: ${nameOf(entry.winnerId)} def. ${nameOf(loserId)}`
      }
      const b = match.playerBId ? nameOf(match.playerBId) : 'BYE'
      return `Round ${round.roundNumber}: ${nameOf(match.playerAId)} vs ${b}`
    }

    default:
      return entry.entityType
  }
}
