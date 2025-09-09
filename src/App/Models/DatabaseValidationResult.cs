using System;
using System.Collections.Generic;

namespace App.Models;

/// <summary>
/// Result of database validation operations.
/// Domain model with no infrastructure dependencies.
/// </summary>
public class DatabaseValidationResult
{
    public int WorkerIndex { get; set; }
    public bool IsValid { get; set; }
    public List<string> Issues { get; set; } = new List<string>();
    public DatabaseStats Stats { get; set; } = null!;
    public string ValidationType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}