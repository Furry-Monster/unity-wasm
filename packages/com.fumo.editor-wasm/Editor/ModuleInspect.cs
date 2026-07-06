using System;
using System.Collections.Generic;
using Wasmtime;

namespace Fumo.EditorWasm
{
    public sealed class ModuleInspect
    {
        public string[] Imports { get; }
        public string[] Exports { get; }

        ModuleInspect(string[] imports, string[] exports)
        {
            Imports = imports;
            Exports = exports;
        }

        public static ModuleInspect FromModule(Module module)
        {
            if (module == null)
                return new ModuleInspect(Array.Empty<string>(), Array.Empty<string>());

            var imports = new List<string>();
            foreach (var import in module.Imports)
                imports.Add($"{import.ModuleName}.{import.Name}");

            var exports = new List<string>();
            foreach (var export in module.Exports)
                exports.Add(export.Name);

            imports.Sort(StringComparer.Ordinal);
            exports.Sort(StringComparer.Ordinal);
            return new ModuleInspect(imports.ToArray(), exports.ToArray());
        }
    }
}
