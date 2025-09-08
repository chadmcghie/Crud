using System;

namespace App.Behaviors;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public class CacheableAttribute : Attribute
{
    public int DurationInSeconds { get; }
    public bool VaryByUserId { get; }
    public string? CustomCacheKeyPrefix { get; }

    public CacheableAttribute(int durationInSeconds = 300, bool varyByUserId = false, string? customCacheKeyPrefix = null)
    {
        DurationInSeconds = durationInSeconds > 0 ? durationInSeconds : 300;
        VaryByUserId = varyByUserId;
        CustomCacheKeyPrefix = customCacheKeyPrefix;
    }
}