using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Exports editor-api WIT contracts as JSON Schema for AI agents.
    /// Note: hand-maintained until M2 generates this from host-imports.v1.json.
    /// </summary>
    public static class SchemaExporter
    {
        public static string DefaultOutputPath
        {
            get
            {
                var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
                var repoRoot = Path.GetFullPath(Path.Combine(projectRoot ?? ".", ".."));
                var schemasDir = Directory.Exists(Path.Combine(repoRoot, "schemas"))
                    ? Path.Combine(repoRoot, "schemas")
                    : Path.Combine(projectRoot ?? ".", "schemas");
                return Path.Combine(schemasDir, "editor-api.schema.json");
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
            var schema = new SchemaDocument
            {
                abi = "editor-api/1",
                description = "Unity Editor WASM host API for tool agents.",
                tools = new List<SchemaFunction>
                {
                    Fn("editor_core.log", "Write a message to the Unity console.", "void", new[] { Param("level", "integer"), Param("message", "string") }),
                    Fn("editor_core.log_error", "Write an error to the Unity console.", "void", new[] { Param("message", "string") }),
                    Fn("editor_core.get_editor_time", "Unity EditorApplication.timeSinceStartup.", "number", Array.Empty<SchemaParam>()),
                    Fn("editor_selection.get_active_object", "Primary selected object handle.", "integer", Array.Empty<SchemaParam>()),
                    Fn("editor_selection.get_active_asset_path", "Asset path of active selection.", "string", Array.Empty<SchemaParam>()),
                    Fn("editor_selection.get_object_name", "Object name for handle.", "string", new[] { Param("handle", "integer") }),
                    Fn("editor_assets.find_assets", "Find assets by filter.", "array", new[] { Param("filter", "string"), Param("search_paths", "array") }),
                    Fn("editor_assets.load_text_asset", "Load TextAsset contents.", "string", new[] { Param("path", "string") }),
                    Fn("editor_assets.asset_exists", "Whether asset exists.", "boolean", new[] { Param("path", "string") }),
                }
            };

            return JsonUtility.ToJson(schema, prettyPrint: true);
        }

        static SchemaFunction Fn(string name, string description, string returns, SchemaParam[] parameters) =>
            new SchemaFunction { name = name, description = description, returns = returns, parameters = parameters };

        static SchemaParam Param(string name, string type) => new SchemaParam { name = name, type = type };

        [Serializable]
        class SchemaDocument
        {
            public string abi;
            public string description;
            public List<SchemaFunction> tools;
        }

        [Serializable]
        class SchemaFunction
        {
            public string name;
            public string description;
            public string returns;
            public SchemaParam[] parameters;
        }

        [Serializable]
        class SchemaParam
        {
            public string name;
            public string type;
        }
    }
}
