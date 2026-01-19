using DraftApp.Api.Models.Requests;
using DraftApp.Api.Models.Responses;
using DraftApp.Api.Services;
using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(JoinEventResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinEvent([FromBody] JoinEventRequest request, CancellationToken ct)
    {
        var response = await eventService.JoinEventAsync(request, ct);
        if (response is null)
        {
            return NotFound(new ErrorResponse { Code = "EVENT_NOT_FOUND", Message = "Event not found or not accepting players" });
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
    /// Finalizes a match result.
    /// </summary>
    [HttpPost("{eventId:guid}/matches/{matchId:guid}/finalize")]
    [ProducesResponseType(typeof(MutationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> FinalizeMatch(Guid eventId, Guid matchId, [FromBody] FinalizeMatchRequest request, CancellationToken ct)
    {
        var authResult = await AuthorizeHostAsync(eventId, ct);
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

    private IActionResult HandleMutationResponse(MutationResponse response)
    {
        if (!response.Success)
        {
            if (response.Error == "Event not found")
            {
                return NotFound(new ErrorResponse { Code = "EVENT_NOT_FOUND", Message = response.Error });
            }

            if (response.Error == "Version conflict")
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
