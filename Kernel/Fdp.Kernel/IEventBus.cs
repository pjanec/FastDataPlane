using System;

namespace Fdp.Kernel
{
    public interface IEventBus
    {
        void Publish<T>(T evt) where T : unmanaged;
        void PublishManaged<T>(T evt) where T : class;
    }
}
