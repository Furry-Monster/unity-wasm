using UnityEditor;

namespace Fumo.EditorWasm
{
    public static class HostImportVerbose
    {
        const string PrefKey = "Fumo.EditorWasm.VerboseHostImportLog";

        public static bool Enabled
        {
            get => EditorPrefs.GetBool(PrefKey, false);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        public static void Toggle()
        {
            Enabled = !Enabled;
            UnityEngine.Debug.Log($"[WasmEditor] Verbose host import log: {(Enabled ? "ON" : "OFF")}");
        }
    }
}
