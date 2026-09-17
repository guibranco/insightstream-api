namespace InsightStream.IntegrationTests;

/// <summary>
/// Forces every integration test class onto one shared <see cref="IntegrationTestFactory"/>/
/// Postgres container instead of each spinning up (and racing over) its own — the factory
/// overrides "ConnectionStrings:Postgres" via a process-wide environment variable, which is only
/// safe if a single instance exists at a time.
/// </summary>
[CollectionDefinition(Name)]
public class IntegrationTestCollection : ICollectionFixture<IntegrationTestFactory>
{
    public const string Name = "Integration";
}
