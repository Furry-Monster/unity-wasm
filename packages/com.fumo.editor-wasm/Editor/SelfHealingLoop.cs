using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Self-healing loop prototype: captures trap reports for automated fix workflows.
    /// </summary>
    public static class SelfHealingLoop
    {
        [Serializable]
        public struct FixRequest
        {
            public string toolId;
            public string trapJson;
            public string suggestedAction;
        }

        public static FixRequest BuildFixRequest(ToolManifest manifest, TrapReport report)
        {
            return new FixRequest
            {
                toolId = manifest?.id,
                trapJson = report?.ToJson(),
                suggestedAction = "Analyze trapJson, patch guest source, rebuild tool.wasm, hot reload."
            };
        }

        public static void WriteFixRequest(FixRequest request, string outputPath = null)
        {
            outputPath ??= Path.Combine(Path.GetTempPath(), "unity-wasm-fix-request.json");
            File.WriteAllText(outputPath, JsonUtility.ToJson(request, prettyPrint: true));
            Debug.Log($"[WasmEditor] Wrote fix request to {outputPath}");
        }

        public static IEnumerable<FixRequest> CollectPendingTraps(IEnumerable<ToolManifest> tools, HotReloadService hotReload)
        {
            foreach (var tool in tools)
            {
                var host = hotReload?.GetHost(tool.id);
                if (host?.LastTrapReport != null)
                    yield return BuildFixRequest(tool, host.LastTrapReport);
            }
        }
    }
}
