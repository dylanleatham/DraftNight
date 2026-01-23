import { useEffect, useRef, useState, useCallback } from 'react'
import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from '@microsoft/signalr'
import type { EventSnapshotResponse } from '../api/types'

export type ConnectionStatus =
  | 'connecting'
  | 'connected'
  | 'reconnecting'
  | 'disconnected'

interface UseEventConnectionOptions {
  eventId: string
  onSnapshot: (snapshot: EventSnapshotResponse) => void
  onError?: (code: string, message: string) => void
}

interface UseEventConnectionResult {
  status: ConnectionStatus
  requestSnapshot: () => Promise<void>
}

export function useEventConnection({
  eventId,
  onSnapshot,
  onError,
}: UseEventConnectionOptions): UseEventConnectionResult {
  const [status, setStatus] = useState<ConnectionStatus>('disconnected')
  const connectionRef = useRef<HubConnection | null>(null)
  const eventIdRef = useRef(eventId)

  // Keep eventId ref updated
  useEffect(() => {
    eventIdRef.current = eventId
  }, [eventId])

  // Request fresh snapshot
  const requestSnapshot = useCallback(async () => {
    const connection = connectionRef.current
    if (connection?.state === HubConnectionState.Connected) {
      try {
        await connection.invoke('RequestSnapshot', eventIdRef.current)
      } catch (err) {
        console.error('Failed to request snapshot:', err)
      }
    }
  }, [])

  useEffect(() => {
    const hubUrl = `${window.location.origin}/hubs/event`

    const connection = new HubConnectionBuilder()
      .withUrl(hubUrl)
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(LogLevel.Warning)
      .build()

    connectionRef.current = connection

    // Handle snapshot updates
    connection.on('EventUpdated', (snapshot: EventSnapshotResponse) => {
      onSnapshot(snapshot)
    })

    // Handle errors
    connection.on('Error', (code: string, message: string) => {
      onError?.(code, message)
    })

    // Connection state changes
    connection.onreconnecting(() => {
      setStatus('reconnecting')
    })

    connection.onreconnected(async () => {
      setStatus('connected')
      // Rejoin the event group after reconnection
      try {
        await connection.invoke('JoinEventGroup', eventIdRef.current)
      } catch (err) {
        console.error('Failed to rejoin event group:', err)
      }
    })

    connection.onclose(() => {
      setStatus('disconnected')
    })

    // Start connection
    const startConnection = async () => {
      setStatus('connecting')
      try {
        await connection.start()
        setStatus('connected')
        // Join the event group
        await connection.invoke('JoinEventGroup', eventId)
      } catch (err) {
        console.error('Failed to connect to SignalR hub:', err)
        setStatus('disconnected')
      }
    }

    startConnection()

    // Request snapshot on visibility change (tab switch)
    const handleVisibilityChange = () => {
      if (
        document.visibilityState === 'visible' &&
        connection.state === HubConnectionState.Connected
      ) {
        connection
          .invoke('RequestSnapshot', eventIdRef.current)
          .catch(console.error)
      }
    }

    document.addEventListener('visibilitychange', handleVisibilityChange)

    // Cleanup
    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      connection.off('EventUpdated')
      connection.off('Error')
      if (connection.state === HubConnectionState.Connected) {
        connection.invoke('LeaveEventGroup', eventIdRef.current).catch(() => {})
      }
      connection.stop()
    }
  }, [eventId, onSnapshot, onError])

  return { status, requestSnapshot }
}
