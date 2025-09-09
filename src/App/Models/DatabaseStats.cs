using System;

namespace App.Models;

/// <summary>
/// Database statistics for debugging and monitoring.
/// Domain model with no infrastructure dependencies.
/// </summary>
public class DatabaseStats
{
    public int PeopleCount { get; set; }
    public int RolesCount { get; set; }
    public int WallsCount { get; set; }
    public int WindowsCount { get; set; }
    public int UsersCount { get; set; }
    public int PasswordResetTokensCount { get; set; }
    public string? ConnectionString { get; set; }
    public bool CanConnect { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}