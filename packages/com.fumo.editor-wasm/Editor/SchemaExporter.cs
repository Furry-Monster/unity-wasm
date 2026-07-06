using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Exports editor-api schema for AI agents from host-imports.v1.json.
    /// </summary>
    public static class SchemaExporter
    {
        [Serializable]
        class ManifestRoot
        {
            public string abi;
            public string description;
            public ManifestImport[] imports;
        }

        [Serializable]
        class ManifestImport
        {
            public string module;
            public string name;
            public string[] @params;
            public string[] returns;
            public int tier;
        }

        public static string DefaultOutputPath
        {
            get
            {
                var manifestPath = Generator.HostImportRegistryGenerator.ResolveManifestPath(
                    Generator.HostBindingGenerator.FindPackageRoot());
                var schemasDir = Path.GetDirectoryName(manifestPath);
                return Path.Combine(schemasDir ?? ".", "editor-api.schema.json");
            }
        }

        public static void ExportToDefaultPath()
        {
            var path = DefaultOutputPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            File.WriteAllText(path, BuildSchemaJson(), Encoding.UTF8);
            Debug.Log($"[WasmEditor] Exported API schema to {path}");
        }

        public static string BuildSchemaJson()
        {
            var manifestPath = Generator.HostImportRegistryGenerator.ResolveManifestPath(
                Generator.HostBindingGenerator.FindPackageRoot());
            var json = File.ReadAllText(manifestPath);
            var manifest = JsonUtility.FromJson<ManifestRoot>(json);
            if (manifest?.imports == null)
                throw new InvalidOperationException($"Invalid manifest: {manifestPath}");

            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine($"  \"abi\": \"{manifest.abi}\",");
            sb.AppendLine($"  \"description\": \"{Escape(manifest.description)}\",");
            sb.AppendLine("  \"imports\": [");

            for (var i = 0; i < manifest.imports.Length; i++)
            {
                var entry = manifest.imports[i];
                sb.AppendLine("    {");
                sb.AppendLine($"      \"name\": \"{entry.module}.{entry.name}\",");
                sb.AppendLine($"      \"tier\": {entry.tier},");
                sb.AppendLine($"      \"params\": {ToJsonArray(entry.@params)},");
                sb.AppendLine($"      \"returns\": {ToJsonArray(entry.returns)}");
                sb.Append(i == manifest.imports.Length - 1 ? "    }" : "    },");
                sb.AppendLine();
            }

            sb.AppendLine("  ]");
            sb.AppendLine("}");
            return sb.ToString();
        }

        static string ToJsonArray(string[] values)
        {
            if (values == null || values.Length == 0)
                return "[]";
            return "[\"" + string.Join("\", \"", values) + "\"]";
        }

        static string Escape(string value) =>
            (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
