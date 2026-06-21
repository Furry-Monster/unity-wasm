using System;
using System.IO;
using UnityEngine;

namespace Fumo.EditorWasm
{
    [Serializable]
    public sealed class ToolManifest
    {
        public string id;
        public string name;
        public string version = "1.0.0";
        public string abi = "editor-api/1";
        public string entry = "bin/tool.wasm";
        public string menu;
        public string shortcut;
        public ToolExportMap exports = new();

        [NonSerialized] public string rootPath;

        public string WasmPath => Path.GetFullPath(Path.Combine(rootPath, entry));

        public string ToolJsonPath => Path.Combine(rootPath, "tool.json");
    }

    [Serializable]
    public sealed class ToolExportMap
    {
        public string on_init = "on_init";
        public string on_shutdown = "on_shutdown";
        public string on_menu_click = "on_menu_click";
        public string on_selection_changed;
    }
}
