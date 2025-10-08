using Xunit;

// Configure xUnit to run integration tests sequentially to avoid database conflicts
// This ensures transaction rollback isolation works properly
[assembly: CollectionBehavior(DisableTestParallelization = true)]
