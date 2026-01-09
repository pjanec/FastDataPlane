using System;

namespace Fdp.Kernel
{
    /// <summary>
    /// Marks a component type as transient (non-snapshotable).
    /// Transient components are excluded from all snapshot operations (GDB, SoD, Flight Recorder).
    /// 
    /// <para><b>Use Cases:</b></para>
    /// <list type="bullet">
    ///   <item>Mutable managed components (Dictionary, List) that are main-thread only</item>
    ///   <item>Heavy caches (UI render caches, texture caches) that don't need snapshots</item>
    ///   <item>Debug/editor-only data that shouldn't be in recordings</item>
    ///   <item>Temporary calculation buffers</item>
    /// </list>
    /// 
    /// <para><b>Thread Safety:</b></para>
    /// Transient components are ONLY accessible on the main thread (World A).
    /// Background modules (World B/C) will never see transient components.
    /// 
    /// <para><b>Example:</b></para>
    /// <code>
    /// [TransientComponent]
    /// public class UIRenderCache
    /// {
    ///     public Dictionary&lt;int, Texture&gt; Cache; // Safe: main-thread only
    /// }
    /// </code>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false, Inherited = false)]
    public sealed class TransientComponentAttribute : Attribute
    {
    }
}
