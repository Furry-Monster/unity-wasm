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

        public static IReadOnlyList<string> GetMissingImports(Module module)
        {
            if (module == null)
                return Array.Empty<string>();

            var required = new HashSet<string>(StringComparer.Ordinal);
            foreach (var import in module.Imports)
                required.Add($"{import.ModuleName}.{import.Name}");

            var hostKeys = new HashSet<string>(HostImportRegistry.Imports.Select(i => i.Key), StringComparer.Ordinal);
            var missing = new List<string>();
            foreach (var key in required)
            {
                if (!hostKeys.Contains(key))
                    missing.Add(key);
            }

            missing.Sort(StringComparer.Ordinal);
            return missing;
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
    }
}
