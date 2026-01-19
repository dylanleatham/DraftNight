namespace DraftApp.Engine.Errors;

/// <summary>
/// Cannot reopen a BYE match.
/// </summary>
public sealed record CannotReopenByeError()
    : EngineError("Cannot reopen a BYE match.");
