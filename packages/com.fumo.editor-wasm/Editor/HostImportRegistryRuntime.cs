using System;
using System.Collections.Generic;
using System.Linq;
using Fumo.EditorWasm.Generated;
using UnityEngine;
using Wasmtime;

namespace Fumo.EditorWasm
{
    public static class HostImportRegistryRuntime
    {
        public static void AssertAllRegistered(IReadOnlyCollection<string> registeredKeys)
        {
            var missing = HostImportRegistry.Imports
                .Select(i => i.Key)
                .Except(registeredKeys, StringComparer.Ordinal)
                .ToList();

            if (missing.Count == 0)
                return;

            var message = "[WasmEditor] HostImportRegistry missing registrations: " + string.Join(", ", missing);
            Debug.LogError(message);
#if UNITY_EDITOR
            throw new InvalidOperationException(message);
#endif
        }

        public static bool ValidateGuestImports(Module module, out string error)
        {
            error = null;
            if (module == null)
            {
                error = "Module is null.";
                return false;
            }

            var hostKeys = new HashSet<string>(HostImportRegistry.Imports.Select(i => i.Key), StringComparer.Ordinal);
            foreach (var import in module.Imports)
            {
                var key = $"{import.ModuleName}.{import.Name}";
                if (!hostKeys.Contains(key))
                {
                    error = $"Guest imports unknown host function '{key}'.";
                    return false;
                }
            }

            return true;
        }

        public static bool ValidateGuestExports(Module module, ToolManifest manifest, out string error)
        {
            error = null;
            if (module == null)
            {
                error = "Module is null.";
                return false;
            }

            if (manifest?.exports == null)
            {
                error = "Tool manifest exports map is missing.";
                return false;
            }

            var exportNames = new HashSet<string>(StringComparer.Ordinal);
            foreach (var export in module.Exports)
                exportNames.Add(export.Name);

            var required = new[]
            {
                manifest.exports.on_init,
                manifest.exports.on_menu_click,
            };

            foreach (var name in required)
            {
                if (string.IsNullOrEmpty(name))
                    continue;

                if (!exportNames.Contains(name))
                {
                    error = $"Guest module missing required export '{name}'.";
                    return false;
                }
            }

            return true;
        }
    }
}
