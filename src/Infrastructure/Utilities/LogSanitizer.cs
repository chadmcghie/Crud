using System;
using System.Text.RegularExpressions;

namespace Infrastructure.Utilities;

/// <summary>
/// Utility class for sanitizing user input before logging to prevent log injection attacks
/// </summary>
public static class LogSanitizer
{
    /// <summary>
    /// Sanitizes a string value for safe logging by removing/replacing potentially harmful characters
    /// </summary>
    /// <param name="value">The value to sanitize</param>
    /// <returns>Sanitized value safe for logging</returns>
    public static string? Sanitize(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        // Remove or replace characters that could be used for log injection
        // This includes line breaks, carriage returns, and other control characters
        var sanitized = Regex.Replace(value, @"[\r\n\t\f\v\0]", "_", RegexOptions.Compiled);

        // Limit length to prevent log spam
        if (sanitized.Length > 500)
        {
            sanitized = sanitized.Substring(0, 500) + "...";
        }

        return sanitized;
    }

    /// <summary>
    /// Sanitizes a cache key for safe logging
    /// </summary>
    /// <param name="key">The cache key to sanitize</param>
    /// <returns>Sanitized key safe for logging</returns>
    public static string? SanitizeKey(string? key)
    {
        return Sanitize(key);
    }

    /// <summary>
    /// Sanitizes a cache pattern for safe logging
    /// </summary>
    /// <param name="pattern">The cache pattern to sanitize</param>
    /// <returns>Sanitized pattern safe for logging</returns>
    public static string? SanitizePattern(string? pattern)
    {
        return Sanitize(pattern);
    }
}
