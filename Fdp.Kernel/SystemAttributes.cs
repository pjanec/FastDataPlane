using System;

namespace Fdp.Kernel
{
    /// <summary>
    /// Specifies the system group a system belongs to.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class UpdateInGroupAttribute : Attribute
    {
        public Type GroupType { get; }
        
        public UpdateInGroupAttribute(Type groupType)
        {
            if (!typeof(SystemGroup).IsAssignableFrom(groupType))
            {
                throw new ArgumentException($"Type {groupType.Name} must derive from SystemGroup", nameof(groupType));
            }
            GroupType = groupType;
        }
    }

    /// <summary>
    /// Specifies that this system should run before the target system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UpdateBeforeAttribute : Attribute
    {
        public Type Target { get; }
        
        public UpdateBeforeAttribute(Type target)
        {
            if (!typeof(ComponentSystem).IsAssignableFrom(target))
            {
                throw new ArgumentException($"Type {target.Name} must derive from ComponentSystem", nameof(target));
            }
            Target = target;
        }
    }

    /// <summary>
    /// Specifies that this system should run after the target system.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class UpdateAfterAttribute : Attribute
    {
        public Type Target { get; }
        
        public UpdateAfterAttribute(Type target)
        {
            if (!typeof(ComponentSystem).IsAssignableFrom(target))
            {
                throw new ArgumentException($"Type {target.Name} must derive from ComponentSystem", nameof(target));
            }
            Target = target;
        }
    }
}
