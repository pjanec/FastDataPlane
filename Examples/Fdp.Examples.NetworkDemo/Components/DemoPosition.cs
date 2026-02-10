using System.Numerics;
using FDP.Interfaces.Abstractions;
using Fdp.Examples.NetworkDemo.Descriptors;

namespace Fdp.Examples.NetworkDemo.Components
{
    [FdpDescriptor(DemoDescriptors.Physics, "DemoPosition")]
    public struct DemoPosition
    {
        public Vector3 Value;
    }
}
