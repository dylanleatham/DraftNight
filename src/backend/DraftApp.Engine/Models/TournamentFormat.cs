namespace DraftApp.Engine.Models;

/// <summary>
/// Tournament format based on player count.
/// RoundRobin for N in {2,3,4}, Swiss for N in {5,6,7,8}.
/// </summary>
public enum TournamentFormat
{
    RoundRobin,
    Swiss
}
