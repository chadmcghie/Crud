using System.Runtime.CompilerServices;

// Only expose internals to specific test assembly for unit testing
// This should be tightly controlled and only used for legitimate testing purposes
[assembly: InternalsVisibleTo("Tests.Unit.Backend")]

// Additional security: Consider removing this attribute in production builds
// by using conditional compilation if needed:
// #if DEBUG
// [assembly: InternalsVisibleTo("Tests.Unit.Backend")]
// #endif
