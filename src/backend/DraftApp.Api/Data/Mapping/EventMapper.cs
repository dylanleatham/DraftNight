using System.Collections.Immutable;
using System.Text.Json;
using DraftApp.Api.Data.Entities;
using DraftApp.Api.Data.Enums;
using DraftApp.Engine.Models;
using Microsoft.EntityFrameworkCore;

namespace DraftApp.Api.Data.Mapping;

/// <summary>
/// Maps between engine state (immutable records) and persistence entities.
/// </summary>
public static class EventMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Converts an EventEntity with all related data to an engine EventState.
    /// </summary>
    public static EventState ToEngineState(EventEntity entity)
    {
        var players = entity.Players
            .ToImmutableDictionary(
                p => p.Id.ToString(),
                p => ToEnginePlayer(p));

        var matchesByRound = entity.Rounds
            .Where(r => r.Matches.Count > 0)
            .ToImmutableDictionary(
                r => r.RoundNumber,
                r => r.Matches
                    .OrderBy(m => m.MatchCode)
                    .Select(m => ToEngineMatch(m))
                    .ToImmutableList());

        var prizeAllocations = entity.PrizeAllocations
            .ToImmutableDictionary(
                pa => pa.PlayerId.ToString(),
                pa => pa.PacksAwarded);

        return new EventState
        {
            EventId = entity.Id.ToString(),
            Players = players,
            PacksInBox = entity.PacksInBox,
            PrizePacks = entity.PrizePacks,
            Format = entity.Format,
            TotalRounds = entity.TotalRounds,
            MatchesByRound = matchesByRound,
            PrizeAllocations = prizeAllocations,
            PrizesAllocated = entity.PrizesAllocated
        };
    }

    /// <summary>
    /// Converts an engine EventState to entity updates.
    /// This method updates an existing entity rather than creating a new one.
    /// </summary>
    public static void UpdateEntityFromState(
        EventEntity entity,
        EventState state,
        DateTime timestamp)
    {
        // Update event-level fields
        entity.PacksInBox = state.PacksInBox;
        entity.PrizePacks = state.PrizePacks;
        entity.Format = state.Format;
        entity.TotalRounds = state.TotalRounds;
        entity.PrizesAllocated = state.PrizesAllocated;
        entity.UpdatedAt = timestamp;

        // Update status based on state
        if (state.PrizesAllocated)
        {
            entity.Status = EventStatus.Completed;
        }
        else if (state.CurrentRound > 0)
        {
            entity.Status = EventStatus.Active;
        }

        // Build lookup for existing entities by string ID
        var existingPlayers = entity.Players.ToDictionary(p => p.Id.ToString());
        var existingRounds = entity.Rounds.ToDictionary(r => r.RoundNumber);

        // Update players
        foreach (var (playerId, player) in state.Players)
        {
            if (existingPlayers.TryGetValue(playerId, out var playerEntity))
            {
                UpdatePlayerEntity(playerEntity, player);
            }
            else
            {
                // New player (shouldn't happen often, but handle it)
                var newPlayer = CreatePlayerEntity(player, entity.Id);
                entity.Players.Add(newPlayer);
            }
        }

        // Update rounds and matches
        foreach (var (roundNumber, matches) in state.MatchesByRound)
        {
            RoundEntity roundEntity;
            if (existingRounds.TryGetValue(roundNumber, out var existing))
            {
                roundEntity = existing;
            }
            else
            {
                roundEntity = new RoundEntity
                {
                    Id = Guid.NewGuid(),
                    EventId = entity.Id,
                    RoundNumber = roundNumber,
                    Status = RoundStatus.PairingsPublished,
                    PublishedAt = timestamp
                };
                entity.Rounds.Add(roundEntity);
            }

            // Check if round is complete
            var allMatchesComplete = matches.All(m => m.IsComplete || (m.IsBye && m.WinnerId is null));
            if (allMatchesComplete && roundEntity.Status != RoundStatus.Closed)
            {
                roundEntity.Status = RoundStatus.Closed;
                roundEntity.ClosedAt = timestamp;
            }

            // Build lookup for existing matches
            var existingMatches = roundEntity.Matches.ToDictionary(m => m.MatchCode);

            foreach (var match in matches)
            {
                if (existingMatches.TryGetValue(match.Id, out var matchEntity))
                {
                    UpdateMatchEntity(matchEntity, match, timestamp);
                }
                else
                {
                    var newMatch = CreateMatchEntity(match, roundEntity.Id, timestamp);
                    roundEntity.Matches.Add(newMatch);
                }
            }
        }

        // Update prize allocations
        if (state.PrizesAllocated)
        {
            var existingAllocations = entity.PrizeAllocations.ToDictionary(pa => pa.PlayerId.ToString());

            foreach (var (playerId, packs) in state.PrizeAllocations)
            {
                if (!existingAllocations.ContainsKey(playerId))
                {
                    entity.PrizeAllocations.Add(new PrizeAllocationEntity
                    {
                        Id = Guid.NewGuid(),
                        EventId = entity.Id,
                        PlayerId = Guid.Parse(playerId),
                        PacksAwarded = packs
                    });
                }
            }
        }
    }

    /// <summary>
    /// Creates a new EventEntity from an initial engine state.
    /// </summary>
    public static EventEntity CreateEntityFromState(
        EventState state,
        string name,
        string? joinCode,
        string hostPinHash,
        string hostToken,
        DateTime timestamp)
    {
        var eventId = Guid.Parse(state.EventId);

        var entity = new EventEntity
        {
            Id = eventId,
            Name = name,
            Status = EventStatus.Setup,
            JoinCode = joinCode,
            PacksInBox = state.PacksInBox,
            PrizePacks = state.PrizePacks,
            Format = state.Format,
            TotalRounds = state.TotalRounds,
            PrizesAllocated = false,
            Version = 1,
            CreatedAt = timestamp,
            UpdatedAt = timestamp,
            HostPinHash = hostPinHash,
            HostToken = hostToken
        };

        // Add players
        foreach (var (_, player) in state.Players)
        {
            entity.Players.Add(CreatePlayerEntity(player, eventId));
        }

        return entity;
    }

    private static Player ToEnginePlayer(PlayerEntity entity)
    {
        var opponents = JsonSerializer.Deserialize<List<string>>(entity.OpponentsJson, JsonOptions)
            ?? new List<string>();

        var lastPlayedRound = JsonSerializer.Deserialize<Dictionary<string, int>>(entity.LastPlayedRoundJson, JsonOptions)
            ?? new Dictionary<string, int>();

        return new Player
        {
            Id = entity.Id.ToString(),
            Name = entity.Name,
            Seed = entity.Seed,
            MatchWins = entity.MatchWins,
            MatchLosses = entity.MatchLosses,
            ByeReceived = entity.ByeReceived,
            IsDropped = entity.IsDropped,
            Opponents = opponents.ToImmutableList(),
            LastPlayedRound = lastPlayedRound.ToImmutableDictionary()
        };
    }

    private static Match ToEngineMatch(MatchEntity entity)
    {
        return new Match
        {
            Id = entity.MatchCode,
            Round = entity.RoundNumber,
            PlayerAId = entity.PlayerAId.ToString(),
            PlayerBId = entity.PlayerBId?.ToString(),
            WinnerId = entity.WinnerId?.ToString()
        };
    }

    private static PlayerEntity CreatePlayerEntity(Player player, Guid eventId)
    {
        return new PlayerEntity
        {
            Id = Guid.Parse(player.Id),
            EventId = eventId,
            Name = player.Name,
            Seed = player.Seed,
            MatchWins = player.MatchWins,
            MatchLosses = player.MatchLosses,
            ByeReceived = player.ByeReceived,
            IsDropped = player.IsDropped,
            OpponentsJson = JsonSerializer.Serialize(player.Opponents.ToList(), JsonOptions),
            LastPlayedRoundJson = JsonSerializer.Serialize(player.LastPlayedRound.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), JsonOptions)
        };
    }

    private static void UpdatePlayerEntity(PlayerEntity entity, Player player)
    {
        entity.MatchWins = player.MatchWins;
        entity.MatchLosses = player.MatchLosses;
        entity.ByeReceived = player.ByeReceived;
        entity.IsDropped = player.IsDropped;
        entity.OpponentsJson = JsonSerializer.Serialize(player.Opponents.ToList(), JsonOptions);
        entity.LastPlayedRoundJson = JsonSerializer.Serialize(player.LastPlayedRound.ToDictionary(kvp => kvp.Key, kvp => kvp.Value), JsonOptions);
    }

    private static MatchEntity CreateMatchEntity(Match match, Guid roundId, DateTime timestamp)
    {
        var entity = new MatchEntity
        {
            Id = Guid.NewGuid(),
            RoundId = roundId,
            MatchCode = match.Id,
            RoundNumber = match.Round,
            PlayerAId = Guid.Parse(match.PlayerAId),
            PlayerBId = match.PlayerBId is not null ? Guid.Parse(match.PlayerBId) : null,
            WinnerId = match.WinnerId is not null ? Guid.Parse(match.WinnerId) : null,
            Status = match.IsComplete ? MatchStatus.Final : MatchStatus.NotStarted
        };

        if (match.IsComplete)
        {
            entity.FinalizedAt = timestamp;
        }

        return entity;
    }

    private static void UpdateMatchEntity(MatchEntity entity, Match match, DateTime timestamp)
    {
        entity.WinnerId = match.WinnerId is not null ? Guid.Parse(match.WinnerId) : null;

        if (match.IsComplete && entity.Status != MatchStatus.Final)
        {
            entity.Status = MatchStatus.Final;
            entity.FinalizedAt = timestamp;
        }
    }
}
