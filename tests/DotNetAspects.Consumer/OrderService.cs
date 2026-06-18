using DotNetAspects.ExternalAspects;

namespace DotNetAspects.Consumer
{
    /// <summary>
    /// A service in a consuming assembly that applies an aspect defined in another assembly.
    /// If weaving works, calls are intercepted by <see cref="CrossAssemblyLogAspect"/>.
    /// </summary>
    public class OrderService
    {
        [CrossAssemblyLogAspect]
        public string PlaceOrder(string product)
        {
            return $"Order placed: {product}";
        }
    }
}
