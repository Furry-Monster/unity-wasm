using System;

namespace Fumo.EditorWasm.Generator
{
    /// <summary>
    /// Marks a static config class that lists C# types exposed as WASM host APIs.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class EditorHostGenConfigAttribute : Attribute
    {
    }

    /// <summary>
    /// Marks a type whose public methods may be exported to WASM tools.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public sealed class EditorHostApiAttribute : Attribute
    {
    }

    /// <summary>
    /// Excludes a member from host API generation.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field)]
    public sealed class EditorHostBlackListAttribute : Attribute
    {
    }
}
