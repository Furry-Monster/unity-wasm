using Fumo.EditorWasm.Generator;

namespace Fumo.EditorWasm
{
    [EditorHostGenConfig]
    public static class EditorHostGenConfig
    {
        // Add [EditorHostApi] to types here or elsewhere in the assembly.
        // Run Tools → Wasm Editor → Generate Host Bindings to refresh the registry.
    }

    [EditorHostApi]
    public static class EditorHostSurfaceSelection
    {
        public static string Describe() => "Selection and active object host surface.";
    }
}
