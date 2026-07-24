global using Xunit;

// Every test class in this project drives one interactive desktop application through a real, focus-stealing
// OS window (see AppiumTestBase), so test classes must never run concurrently: xUnit parallelizes across
// separate test collections by default, and splitting these tests into multiple per-feature-area classes
// (each its own implicit collection) would otherwise launch several app instances simultaneously, fighting
// each other for window focus and the single shared Appium/AT-SPI server connection.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
