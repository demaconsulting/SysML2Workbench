namespace DemaConsulting.SysML2Workbench.IntegrationTests;

/// <summary>
///     Pairs the <c>"AppFixture"</c> collection name with <see cref="AppFixture" /> so xUnit shares one launched
///     application/Appium session across every test class in this assembly decorated with
///     <c>[Collection("AppFixture")]</c>, rather than relaunching the Desktop application per test. Required
///     whenever <c>[Collection]</c> is used - xUnit v3 silently disables parallelism without providing any
///     shared fixture if this definition is omitted (see <c>csharp-testing.md</c>'s xUnit v3 pitfalls).
/// </summary>
[CollectionDefinition("AppFixture")]
public sealed class AppFixtureCollection : ICollectionFixture<AppFixture>;
