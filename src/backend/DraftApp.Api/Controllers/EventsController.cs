using DraftApp.Api.Models;
using DraftApp.Api.Models.Requests;
using DraftApp.Api.Models.Responses;
using DraftApp.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DraftApp.Api.Controllers;

/// <summary>
/// API controller for tournament event operations.
/// </summary>
[ApiController]
[Route("api/events")]
[Produces("application/json")]
public class EventsController(IEventService eventService) : ControllerBase
{
    private const string HostTokenHeader = "X-Host-Token";
    private const string PlayerTokenHeader = "X-Player-Token";

    // ========================================
    // Event Lifecycle APIs (3.1)
    // ========================================

    /// <summary>
    /// Creates a new tournament event.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(CreateEventResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateEvent([FromBody] CreateEventRequest request, CancellationToken ct)
    {
        var response = await eventService.CreateEventAsync(request, ct);
        return CreatedAtAction(nameof(GetEvent), new { eventId = response.EventId }, response);
    }

    /// <summary>
    /// Joins an event as a player.
    /// </summary>
    [HttpPost("join")]
    [EnableRateLimiting(RateLimitPolicies.Join)]
    [ProducesResponseType(typeof(JoinEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> JoinEvent([FromBody] JoinEventRequest request, CancellationToken ct)
    {
        var result = await eventService.JoinEventAsync(request, ct);

        if (result.ErrorCode is not null)
        {
            return result.ErrorCode switch
            {
                JoinEventErrorCode.NotFound => NotFound(new ErrorResponse { Code = "EVENT_NOT_FOUND", Message = "Event not found or not accepting players" }),
                JoinEventErrorCode.LobbyFull => Conflict(new ErrorResponse { Code = "LOBBY_FULL", Message = "Lobby is full (maximum 8 players)" }),
                JoinEventErrorCode.VersionConflict => Conflict(new ErrorResponse { Code = "VERSION_CONFLICT", Message = "Version conflict" }),
                _ => BadRequest(new ErrorResponse { Code = "JOIN_FAILED", Message = "Failed to join event" })
            };
        }

        return Ok(result.Response);
    }

    /// <summary>
    /// Reclaims an existing seat (for example on a new phone) using join code, name and PIN.
    /// Issues new tokens and invalidates the old ones. Rate limited because it checks a PIN.
    /// </summary>
    [HttpPost("resume")]
    [EnableRateLimiting(RateLimitPolicies.Credentials)]
    [ProducesResponseType(typeof(ResumeSessionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ResumeSession([FromBody] ResumeSessionRequest request, CancellationToken ct)
    {
        var response = await eventService.ResumeSessionAsync(request, ct);
        if (response is null)
        {
            // Deliberately doesn't say which of code, name or PIN was wrong
            return Unauthorized(new ErrorResponse { Code = "INVALID_CREDENTIALS", Message = "No seat matches that join code, name and PIN" });
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets an event snapshot by ID.
    /// </summary>
    [HttpGet("{eventId:guid}")]
    [ProducesResponseType(typeof(EventSnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvent(Guid eventId, CancellationToken ct)
    {
        var response = await eventService.GetEventAsync(eventId, ct);
        if (response is null)
        {
            return NotFound(new ErrorResponse { Code = "EVENT_NOT_FOUND", Message = "Event not found" });
        }

        return Ok(response);
    }

    // ========================================
    // Host Control APIs (3.2)
    // ========================================

    /// <summary>
    /// Starts an event and generates round 1 pairings.
    /// </summary>
    [HttpPost("{eventId:guid}/start")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> StartEvent(Guid eventId, [FromBody] HostActionRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.StartEventAsync(eventId, request.ExpectedVersion, ct);
        return HandleMutationResponse(response);
    }

    /// <summary>
    /// Publishes pairings for a round.
    /// </summary>
    [HttpPost("{eventId:guid}/rounds/{roundNumber:int}/publish")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> PublishPairings(Guid eventId, int roundNumber, [FromBody] HostActionRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.PublishPairingsAsync(eventId, roundNumber, request.ExpectedVersion, ct);
        return HandleMutationResponse(response);
    }

    /// <summary>
    /// Finalizes a match result. Can be called by the host or by either player in the match.
    /// </summary>
    [HttpPost("{eventId:guid}/matches/{matchId:guid}/finalize")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinalizeMatch(Guid eventId, Guid matchId, [FromBody] FinalizeMatchRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostOrMatchPlayerAsync(eventId, matchId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.FinalizeMatchAsync(eventId, matchId, request.WinnerId, request.ExpectedVersion, ct);
        return HandleMutationResponse(response);
    }

    /// <summary>
    /// Drops a player from the tournament.
    /// </summary>
    [HttpPost("{eventId:guid}/players/{playerId:guid}/drop")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DropPlayer(Guid eventId, Guid playerId, [FromBody] HostActionRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.DropPlayerAsync(eventId, playerId, request.ExpectedVersion, request.Reason, ct);
        return HandleMutationResponse(response);
    }

    /// <summary>
    /// Allocates prizes after the tournament completes.
    /// </summary>
    [HttpPost("{eventId:guid}/prizes/allocate")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AllocatePrizes(Guid eventId, [FromBody] HostActionRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.AllocatePrizesAsync(eventId, request.ExpectedVersion, ct);
        return HandleMutationResponse(response);
    }

    /// <summary>
    /// Cancels an event. Sets status to Archived and notifies all connected clients.
    /// </summary>
    [HttpPost("{eventId:guid}/cancel")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CancelEvent(Guid eventId, [FromBody] HostActionRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.CancelEventAsync(eventId, request.ExpectedVersion, request.Reason, ct);
        return HandleMutationResponse(response);
    }

    /// <summary>
    /// Allows a player to leave the event. During Setup, removes the player. During Active, drops the player.
    /// The host cannot leave their own event.
    /// </summary>
    [HttpPost("{eventId:guid}/leave")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LeaveEvent(Guid eventId, [FromBody] LeaveEventRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizePlayerAsync(eventId, ct);
        if (authResult.Error is not null)
        {
            return authResult.Error;
        }

        var response = await eventService.LeaveEventAsync(eventId, authResult.PlayerId!.Value, request.ExpectedVersion, ct);
        return HandleMutationResponse(response);
    }

    // ========================================
    // Host Repair APIs (3.3)
    // ========================================

    /// <summary>
    /// Reopens a finalized match for correction.
    /// </summary>
    [HttpPost("{eventId:guid}/matches/{matchId:guid}/reopen")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReopenMatch(Guid eventId, Guid matchId, [FromBody] ReopenMatchRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.ReopenMatchAsync(eventId, matchId, request.ExpectedVersion, request.Reason, ct);
        return HandleMutationResponse(response);
    }

    /// <summary>
    /// Swaps two players between their respective matches in a round.
    /// </summary>
    [HttpPost("{eventId:guid}/rounds/{roundNumber:int}/swap-opponents")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SwapOpponents(Guid eventId, int roundNumber, [FromBody] SwapOpponentsRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.SwapOpponentsAsync(
            eventId,
            roundNumber,
            request.MatchId1,
            request.PlayerId1,
            request.MatchId2,
            request.PlayerId2,
            request.ExpectedVersion,
            request.Reason,
            ct);
        return HandleMutationResponse(response);
    }

    /// <summary>
    /// Reopens all finalized matches in a round.
    /// </summary>
    [HttpPost("{eventId:guid}/rounds/{roundNumber:int}/reopen")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReopenRound(Guid eventId, int roundNumber, [FromBody] ReopenRoundRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.ReopenRoundAsync(eventId, roundNumber, request.ExpectedVersion, request.Reason, ct);
        return HandleMutationResponse(response);
    }

    /// <summary>
    /// Regenerates pairings for a round, deleting existing pairings and creating new ones.
    /// </summary>
    [HttpPost("{eventId:guid}/rounds/{roundNumber:int}/regenerate")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RegeneratePairings(Guid eventId, int roundNumber, [FromBody] RegeneratePairingsRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.RegeneratePairingsAsync(eventId, roundNumber, request.ExpectedVersion, request.Reason, ct);
        return HandleMutationResponse(response);
    }

    // ========================================
    // Read-Only APIs
    // ========================================

    /// <summary>
    /// Gets tournament standings.
    /// </summary>
    [HttpGet("{eventId:guid}/standings")]
    [ProducesResponseType(typeof(StandingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStandings(Guid eventId, CancellationToken ct)
    {
        var response = await eventService.GetStandingsAsync(eventId, ct);
        if (response is null)
        {
            return NotFound(new ErrorResponse { Code = "EVENT_NOT_FOUND", Message = "Event not found" });
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets audit log for an event.
    /// </summary>
    [HttpGet("{eventId:guid}/audit")]
    [ProducesResponseType(typeof(AuditLogResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAuditLog(Guid eventId, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
        if (authResult is not null)
        {
            return authResult;
        }

        var response = await eventService.GetAuditLogAsync(eventId, ct);
        if (response is null)
        {
            return NotFound(new ErrorResponse { Code = "EVENT_NOT_FOUND", Message = "Event not found" });
        }

        return Ok(response);
    }

    // ========================================
    // Helper Methods
    // ========================================
    private async Task<IActionResult?> AuthorizeHostAsync(Guid eventId, CancellationToken ct)
    {
        if (!Request.Headers.TryGetValue(HostTokenHeader, out var tokenValues) || string.IsNullOrEmpty(tokenValues.FirstOrDefault()))
        {
            return Unauthorized(new ErrorResponse { Code = "MISSING_HOST_TOKEN", Message = "Host token required" });
        }

        var hostToken = tokenValues.First()!;
        var authorizedEventId = await eventService.ValidateHostTokenAsync(hostToken, ct);

        if (authorizedEventId is null)
        {
            return Unauthorized(new ErrorResponse { Code = "INVALID_HOST_TOKEN", Message = "Invalid host token" });
        }

        if (authorizedEventId != eventId)
        {
            return Unauthorized(new ErrorResponse { Code = "TOKEN_EVENT_MISMATCH", Message = "Host token does not match event" });
        }

        return null;
    }

    private async Task<(IActionResult? Error, Guid? PlayerId)> AuthorizePlayerAsync(Guid eventId, CancellationToken ct)
    {
        // Check if player token is provided
        if (!Request.Headers.TryGetValue(PlayerTokenHeader, out var tokenValues) || string.IsNullOrEmpty(tokenValues.FirstOrDefault()))
        {
            return (Unauthorized(new ErrorResponse { Code = "MISSING_PLAYER_TOKEN", Message = "Player token required" }), null);
        }

        var playerToken = tokenValues.First()!;

        // Validate the player token and check if player is the host
        var result = await eventService.ValidatePlayerTokenAsync(playerToken, eventId, ct);
        if (result is null)
        {
            return (Unauthorized(new ErrorResponse { Code = "INVALID_PLAYER_TOKEN", Message = "Invalid player token" }), null);
        }

        return (null, result);
    }

    private async Task<IActionResult?> AuthorizeHostOrMatchPlayerAsync(Guid eventId, Guid matchId, CancellationToken ct)
    {
        // First, try host token authorization
        if (Request.Headers.TryGetValue(HostTokenHeader, out var hostTokenValues) && !string.IsNullOrEmpty(hostTokenValues.FirstOrDefault()))
        {
            var hostToken = hostTokenValues.First()!;
            var authorizedEventId = await eventService.ValidateHostTokenAsync(hostToken, ct);

            if (authorizedEventId == eventId)
            {
                return null; // Host is authorized
            }
        }

        // If no valid host token, try player token authorization
        if (Request.Headers.TryGetValue(PlayerTokenHeader, out var playerTokenValues) && !string.IsNullOrEmpty(playerTokenValues.FirstOrDefault()))
        {
            var playerToken = playerTokenValues.First()!;
            var playerId = await eventService.ValidatePlayerTokenForMatchAsync(playerToken, eventId, matchId, ct);

            if (playerId is not null)
            {
                return null; // Player is authorized for this match
            }
        }

        // Neither authorization succeeded
        return Unauthorized(new ErrorResponse { Code = "UNAUTHORIZED", Message = "Valid host token or player token required" });
    }

    private IActionResult HandleMutationResponse(MutationResponse response)
    {
        if (!response.Success)
        {
            if (response.Error == MutationErrors.EventNotFound)
            {
                return NotFound(new ErrorResponse { Code = "EVENT_NOT_FOUND", Message = response.Error });
            }

            if (response.Error == MutationErrors.VersionConflict)
            {
                return Conflict(new ErrorResponse
                {
                    Code = "VERSION_CONFLICT",
                    Message = response.Error,
                    Details = new { CurrentVersion = response.NewVersion }
                });
            }

            return BadRequest(new ErrorResponse { Code = "OPERATION_FAILED", Message = response.Error ?? "Operation failed" });
        }

        return Ok(response);
    }
}
