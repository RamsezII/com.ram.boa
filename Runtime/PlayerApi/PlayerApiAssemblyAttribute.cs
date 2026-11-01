using System;

namespace Boa
{
    /// <summary>Explicitly exposes an assembly's public API to player scripts compiled by BOA.</summary>
    [AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
    public sealed class PlayerApiAssemblyAttribute : Attribute
    {
    }
}
