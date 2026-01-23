import { describe, it, expect } from 'vitest'
import {
  AuditActionType,
  EventStatus,
  RoundStatus,
  MatchStatus,
  TournamentFormat,
} from './types'

describe('API Types', () => {
  describe('AuditActionType', () => {
    it('has correct numeric values matching backend enum', () => {
      expect(AuditActionType.EventCreated).toBe(0)
      expect(AuditActionType.EventStarted).toBe(1)
      expect(AuditActionType.PlayerJoined).toBe(2)
      expect(AuditActionType.PlayerDropped).toBe(3)
      expect(AuditActionType.PairingsGenerated).toBe(4)
      expect(AuditActionType.MatchFinalized).toBe(5)
      expect(AuditActionType.PrizesAllocated).toBe(6)
      expect(AuditActionType.HostRepair).toBe(7)
      expect(AuditActionType.MatchReopened).toBe(8)
    })
  })

  describe('EventStatus', () => {
    it('has correct numeric values', () => {
      expect(EventStatus.Setup).toBe(0)
      expect(EventStatus.Active).toBe(1)
      expect(EventStatus.Completed).toBe(2)
      expect(EventStatus.Archived).toBe(3)
    })
  })

  describe('RoundStatus', () => {
    it('has correct numeric values', () => {
      expect(RoundStatus.Pending).toBe(0)
      expect(RoundStatus.PairingsPublished).toBe(1)
      expect(RoundStatus.Closed).toBe(2)
    })
  })

  describe('MatchStatus', () => {
    it('has correct numeric values', () => {
      expect(MatchStatus.NotStarted).toBe(0)
      expect(MatchStatus.InProgress).toBe(1)
      expect(MatchStatus.Final).toBe(2)
    })
  })

  describe('TournamentFormat', () => {
    it('has correct numeric values', () => {
      expect(TournamentFormat.RoundRobin).toBe(0)
      expect(TournamentFormat.Swiss).toBe(1)
    })
  })
})
