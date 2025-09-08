using System;

namespace App.Behaviors;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class InvalidatesCacheAttribute : Attribute
{
    public Type[] QueryTypes { get; }
    public string? Pattern { get; }
    public bool InvalidateAll { get; }

    public InvalidatesCacheAttribute(params Type[] queryTypes)
    {
        QueryTypes = queryTypes ?? Array.Empty<Type>();
        InvalidateAll = false;
    }

    public InvalidatesCacheAttribute(string pattern)
    {
        Pattern = pattern;
        QueryTypes = Array.Empty<Type>();
        InvalidateAll = false;
    }

    public InvalidatesCacheAttribute(bool invalidateAll = true)
    {
        InvalidateAll = invalidateAll;
        QueryTypes = Array.Empty<Type>();
    }
}