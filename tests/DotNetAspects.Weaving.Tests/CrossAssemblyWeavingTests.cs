using DotNetAspects.Consumer;
using DotNetAspects.ExternalAspects;
using Xunit;

namespace DotNetAspects.Weaving.Tests;

/// <summary>
/// Verifies that the weaver weaves a consuming assembly whose aspect is defined in a *different*
/// assembly, even when the consuming assembly does not reference DotNetAspects directly
/// (regression test for "DotNetAspects types not found. Skipping weaving").
/// </summary>
public class CrossAssemblyWeavingTests
{
    [Fact]
    public void Aspect_DefinedInAnotherAssembly_IsWoven()
    {
        CrossAssemblyLogAspect.Log.Clear();
        var service = new OrderService();

        var result = service.PlaceOrder("Widget");

        Assert.Equal("Order placed: Widget", result);
        Assert.Equal(new[]
        {
            "Before: PlaceOrder",
            "After: PlaceOrder"
        }, CrossAssemblyLogAspect.Log);
    }
}
