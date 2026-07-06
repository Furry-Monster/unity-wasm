using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Exports discovered tools for AI agent consumption.
    /// </summary>
    public static class ToolRegistryExporter
    {
        [Serializable]
        class RegistryDocument
        {
            public string generated_at_utc;
            public List<RegistryEntry> tools = new();
        }

        [Serializable]
        class RegistryEntry
        {
            public string id;
            public string name;
            public string version;
            public string abi;
            public string wasm_path;
            public string menu;
        }

        public static void Export()
        {
            WasmEditorRuntime.EnsureReady();

            var doc = new RegistryDocument
            {
                generated_at_utc = DateTime.UtcNow.ToString("o")
            };

            foreach (var tool in WasmEditorRuntime.Tools)
            {
                doc.tools.Add(new RegistryEntry
                {
                    id = tool.id,
                    name = tool.name,
                    version = tool.version,
                    abi = tool.abi,
                    wasm_path = string.IsNullOrEmpty(tool.entry) ? "bin/tool.wasm" : tool.entry.Replace('\\', '/'),
                    menu = tool.menu
                });
            }

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            var repoRoot = Path.GetFullPath(Path.Combine(projectRoot, ".."));
            var schemasDir = Directory.Exists(Path.Combine(repoRoot, "schemas"))
                ? Path.Combine(repoRoot, "schemas")
                : Path.Combine(projectRoot, "schemas");
            Directory.CreateDirectory(schemasDir);
            var path = Path.Combine(schemasDir, "tool-registry.json");
            File.WriteAllText(path, JsonUtility.ToJson(doc, prettyPrint: true), Encoding.UTF8);
            Debug.Log($"[WasmEditor] Exported tool registry to {path}");
        }
    }
}
