import { useState, useEffect, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { api, ApiError } from '../api/client';
import { useAuth } from '../context/AuthContext';
import { useEvent } from '../context/EventContext';
import { Card, Spinner } from '../components/ui';
import type { AuditLogEntry } from '../api/types';
import { AuditActionType } from '../api/types';
import styles from './AuditLogPage.module.css';

function getActionTypeLabel(actionType: AuditActionType): string {
  switch (actionType) {
    case AuditActionType.EventCreated:
      return 'Event Created';
    case AuditActionType.EventStarted:
      return 'Event Started';
    case AuditActionType.PlayerJoined:
      return 'Player Joined';
    case AuditActionType.PlayerDropped:
      return 'Player Dropped';
    case AuditActionType.PairingsGenerated:
      return 'Pairings Generated';
    case AuditActionType.MatchFinalized:
      return 'Match Finalized';
    case AuditActionType.PrizesAllocated:
      return 'Prizes Allocated';
    case AuditActionType.HostRepair:
      return 'Host Repair';
    case AuditActionType.MatchReopened:
      return 'Match Reopened';
    default:
      return 'Unknown Action';
  }
}

function getActionTypeVariant(actionType: AuditActionType): 'info' | 'success' | 'warning' | 'error' {
  switch (actionType) {
    case AuditActionType.EventCreated:
    case AuditActionType.EventStarted:
    case AuditActionType.PlayerJoined:
      return 'info';
    case AuditActionType.PairingsGenerated:
    case AuditActionType.MatchFinalized:
    case AuditActionType.PrizesAllocated:
      return 'success';
    case AuditActionType.PlayerDropped:
    case AuditActionType.HostRepair:
    case AuditActionType.MatchReopened:
      return 'warning';
    default:
      return 'info';
  }
}

function formatTimestamp(isoString: string): string {
  const date = new Date(isoString);
  return date.toLocaleString(undefined, {
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function getEntityDescription(entry: AuditLogEntry, getPlayerName: (id: string) => string | undefined): string {
  if (entry.entityType === 'Player' && entry.entityId) {
    const name = getPlayerName(entry.entityId);
    return name ? `Player: ${name}` : `Player ID: ${entry.entityId.slice(0, 8)}...`;
  }
  if (entry.entityType === 'Match' && entry.entityId) {
    return `Match: ${entry.entityId.slice(0, 8)}...`;
  }
  if (entry.entityType === 'Round' && entry.entityId) {
    return `Round ${entry.entityId}`;
  }
  if (entry.entityType === 'Event') {
    return '';
  }
  return entry.entityType;
}

export function AuditLogPage() {
  const { eventId } = useParams<{ eventId: string }>();
  const { getHostToken } = useAuth();
  const { getPlayer } = useEvent();

  const [entries, setEntries] = useState<AuditLogEntry[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const hostToken = getHostToken(eventId!);

  const loadAuditLog = useCallback(async () => {
    if (!hostToken) {
      setError('Only the host can view the audit log');
      setLoading(false);
      return;
    }

    try {
      const response = await api.getAuditLog(eventId!, hostToken);
      setEntries(response.entries);
      setError(null);
    } catch (err) {
      if (err instanceof ApiError) {
        setError(err.message);
      } else {
        setError('Failed to load audit log');
      }
    } finally {
      setLoading(false);
    }
  }, [eventId, hostToken]);

  useEffect(() => {
    loadAuditLog();
  }, [loadAuditLog]);

  const getPlayerName = useCallback(
    (playerId: string): string | undefined => {
      const player = getPlayer(playerId);
      return player?.name;
    },
    [getPlayer]
  );

  if (!hostToken) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>Only the host can view the audit log.</p>
          <Link to={`/event/${eventId}/pairings`} className={styles.backLink}>
            &larr; Back to Pairings
          </Link>
        </div>
      </div>
    );
  }

  if (loading) {
    return (
      <div className={styles.container}>
        <div className={styles.loading}>
          <Spinner />
          <p>Loading audit log...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={styles.container}>
        <div className={styles.error}>
          <p>{error}</p>
          <Link to={`/event/${eventId}/pairings`} className={styles.backLink}>
            &larr; Back to Pairings
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className={styles.container}>
      <div className={styles.header}>
        <h2 className={styles.title}>Audit Log</h2>
        <p className={styles.subtitle}>Chronological history of all event actions</p>
      </div>

      {entries.length === 0 ? (
        <div className={styles.empty}>
          <p>No actions recorded yet.</p>
        </div>
      ) : (
        <div className={styles.timeline}>
          {entries.map((entry) => {
            const variant = getActionTypeVariant(entry.actionType);
            const entityDesc = getEntityDescription(entry, getPlayerName);

            return (
              <Card key={entry.id} className={`${styles.entry} ${styles[variant]}`}>
                <div className={styles.entryHeader}>
                  <span className={`${styles.actionType} ${styles[variant]}`}>
                    {getActionTypeLabel(entry.actionType)}
                  </span>
                  <span className={styles.timestamp}>
                    {formatTimestamp(entry.createdAt)}
                  </span>
                </div>
                {entityDesc && (
                  <div className={styles.entityInfo}>{entityDesc}</div>
                )}
                {entry.reason && (
                  <div className={styles.reason}>
                    <span className={styles.reasonLabel}>Reason:</span> {entry.reason}
                  </div>
                )}
              </Card>
            );
          })}
        </div>
      )}
    </div>
  );
}
