namespace DraftApp.Engine.Models;

/// <summary>
/// Tournament format based on player count.
/// RoundRobin for N ∈ {2,3,4}, Swiss for N ∈ {5,6,7,8}
/// </summary>
public enum TournamentFormat
{
    RoundRobin,
    Swiss
}
