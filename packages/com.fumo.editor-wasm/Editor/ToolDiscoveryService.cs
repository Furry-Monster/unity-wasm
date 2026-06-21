using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Discovers tool.json manifests from configured search paths.
    /// </summary>
    public static class ToolDiscoveryService
    {
        static readonly List<string> SearchRoots = new();

        public static IReadOnlyList<string> Roots => SearchRoots;

        static ToolDiscoveryService()
        {
            AddDefaultRoots();
        }

        public static void AddDefaultRoots()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
            if (!string.IsNullOrEmpty(projectRoot))
            {
                AddSearchRoot(Path.Combine(projectRoot, "Packages"));
                AddSearchRoot(Path.Combine(Application.dataPath, "Editor", "Tools"));
            }

            var homeTools = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "UnityEditorTools");
            AddSearchRoot(homeTools);

            // Repo examples/ when using sample-project or embedding unity-wasm.
            var repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            AddSearchRoot(Path.Combine(repoRoot, "examples"));
        }

        public static void AddSearchRoot(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            var full = Path.GetFullPath(path);
            if (!SearchRoots.Contains(full))
                SearchRoots.Add(full);
        }

        public static List<ToolManifest> DiscoverAll()
        {
            var results = new List<ToolManifest>();
            var seen = new HashSet<string>();

            foreach (var root in SearchRoots)
            {
                if (!Directory.Exists(root))
                    continue;

                foreach (var toolJson in Directory.EnumerateFiles(root, "tool.json", SearchOption.AllDirectories))
                {
                    try
                    {
                        var manifest = LoadManifest(toolJson);
                        if (manifest == null || string.IsNullOrEmpty(manifest.id))
                            continue;

                        if (!seen.Add(manifest.id))
                            continue;

                        if (!File.Exists(manifest.WasmPath))
                        {
                            Debug.LogWarning($"[WasmEditor] Skipping '{manifest.id}': WASM not found at {manifest.WasmPath}");
                            continue;
                        }

                        results.Add(manifest);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[WasmEditor] Failed to load {toolJson}: {ex.Message}");
                    }
                }
            }

            return results;
        }

        public static ToolManifest LoadManifest(string toolJsonPath)
        {
            var json = File.ReadAllText(toolJsonPath);
            var manifest = JsonUtility.FromJson<ToolManifest>(json);
            if (manifest == null)
                return null;

            manifest.rootPath = Path.GetDirectoryName(Path.GetFullPath(toolJsonPath));
            return manifest;
        }
    }
}
