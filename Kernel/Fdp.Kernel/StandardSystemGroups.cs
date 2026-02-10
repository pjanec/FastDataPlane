namespace Fdp.Kernel
{
    /// <summary>
    /// Group for systems that run during the initialization phase of a frame.
    /// </summary>
    public class InitializationSystemGroup : SystemGroup { }

    /// <summary>
    /// Group for systems that run during the main simulation logic phase.
    /// </summary>
    public class SimulationSystemGroup : SystemGroup { }

    /// <summary>
    /// Group for systems that run during the presentation/rendering phase.
    /// </summary>
    public class PresentationSystemGroup : SystemGroup { }
}
