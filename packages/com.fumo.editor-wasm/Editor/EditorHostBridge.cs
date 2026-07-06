using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using Wasmtime;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Implements editor-api host imports exposed to WASM tools.
    /// </summary>
    public sealed class EditorHostBridge
    {
        readonly HandlePool<UnityEngine.Object> _objectHandles = new();
        readonly HostCallTrace _trace;
        readonly HashSet<string> _registeredKeys = new(StringComparer.Ordinal);
        Memory _guestMemory;

        public HandlePool<UnityEngine.Object> ObjectHandles => _objectHandles;
        public IReadOnlyCollection<string> RegisteredImportKeys => _registeredKeys;

        public EditorHostBridge(HostCallTrace trace)
        {
            _trace = trace;
        }

        public void SetGuestMemory(Memory memory) => _guestMemory = memory;

        public void ClearHandles() => _objectHandles.Clear();

        public void RegisterImports(Linker linker)
        {
            _registeredKeys.Clear();
            RegisterCoreImports(linker);
            RegisterSelectionImports(linker);
            RegisterAssetImports(linker);
            RegisterSceneImports(linker);
            HostImportRegistryRuntime.AssertAllRegistered(_registeredKeys);
        }

        void RegisterCoreImports(Linker linker)
        {
            Define(linker, "editor_core", "log", (Caller caller, int level, int ptr, int len) =>
            {
                Record("editor_core", "log");
                var message = WasmMemoryBridge.ReadString(GetMemory(caller), ptr, len);
                switch (level)
                {
                    case 1:
                        Debug.LogWarning(message);
                        ToolWindowShell.NotifyLog($"WARN: {message}");
                        break;
                    case 2:
                        Debug.LogError(message);
                        ToolWindowShell.NotifyLog($"ERROR: {message}");
                        break;
                    default:
                        Debug.Log(message);
                        ToolWindowShell.NotifyLog(message);
                        break;
                }
            });

            Define(linker, "editor_core", "log_error", (Caller caller, int ptr, int len) =>
            {
                Record("editor_core", "log_error");
                var message = WasmMemoryBridge.ReadString(GetMemory(caller), ptr, len);
                Debug.LogError(message);
                ToolWindowShell.NotifyLog($"ERROR: {message}");
            });

            Define(linker, "editor_core", "get_editor_time", () =>
            {
                Record("editor_core", "get_editor_time");
                return EditorApplication.timeSinceStartup;
            });

            Define(linker, "editor_core", "show_progress", (Caller caller, int titlePtr, int titleLen, int infoPtr, int infoLen, float progress) =>
            {
                Record("editor_core", "show_progress");
                var memory = GetMemory(caller);
                var title = WasmMemoryBridge.ReadString(memory, titlePtr, titleLen);
                var info = WasmMemoryBridge.ReadString(memory, infoPtr, infoLen);
                EditorUtility.DisplayProgressBar(title, info, progress);
                ToolWindowShell.NotifyProgress(title, info, progress);
            });

            Define(linker, "editor_core", "clear_progress", () =>
            {
                Record("editor_core", "clear_progress");
                EditorUtility.ClearProgressBar();
                ToolWindowShell.NotifyClearProgress();
            });
        }

        void RegisterSelectionImports(Linker linker)
        {
            Define(linker, "editor_selection", "get_active_object", () =>
            {
                Record("editor_selection", "get_active_object");
                _objectHandles.Sweep();
                return (long)_objectHandles.Register(Selection.activeObject);
            });

            Define(linker, "editor_selection", "get_active_objects_count", () =>
            {
                Record("editor_selection", "get_active_objects_count");
                return Selection.objects?.Length ?? 0;
            });

            Define(linker, "editor_selection", "get_active_object_at", (int index) =>
            {
                Record("editor_selection", "get_active_object_at");
                _objectHandles.Sweep();
                var objects = Selection.objects;
                if (objects == null || index < 0 || index >= objects.Length)
                    return 0L;
                return (long)_objectHandles.Register(objects[index]);
            });

            Define(linker, "editor_selection", "get_active_asset_path", (Caller caller, int outPtr, int maxLen) =>
            {
                Record("editor_selection", "get_active_asset_path");
                var path = AssetDatabase.GetAssetPath(Selection.activeObject);
                if (string.IsNullOrEmpty(path))
                    return 0;
                return WasmMemoryBridge.WriteString(GetMemory(caller), outPtr, maxLen, path);
            });

            Define(linker, "editor_selection", "get_object_name", (Caller caller, long handle, int outPtr, int maxLen) =>
            {
                Record("editor_selection", "get_object_name");
                if (!_objectHandles.TryGet((ulong)handle, out var obj) || obj == null)
                    return -1;
                return WasmMemoryBridge.WriteString(GetMemory(caller), outPtr, maxLen, obj.name);
            });
        }

        void RegisterAssetImports(Linker linker)
        {
            Define(linker, "editor_assets", "asset_exists", (Caller caller, int pathPtr, int pathLen) =>
            {
                Record("editor_assets", "asset_exists");
                var path = WasmMemoryBridge.ReadString(GetMemory(caller), pathPtr, pathLen);
                return AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null ? 1 : 0;
            });

            Define(linker, "editor_assets", "find_assets_count", (Caller caller, int filterPtr, int filterLen, int pathsPtr, int pathsLen) =>
            {
                Record("editor_assets", "find_assets_count");
                var memory = GetMemory(caller);
                var filter = WasmMemoryBridge.ReadString(memory, filterPtr, filterLen);
                var searchPaths = ParseNullSeparatedPaths(memory, pathsPtr, pathsLen);
                return AssetDatabase.FindAssets(filter, searchPaths.ToArray()).Length;
            });

            Define(linker, "editor_assets", "find_asset_at", (Caller caller, int filterPtr, int filterLen, int pathsPtr, int pathsLen, int index, int outPtr, int maxLen) =>
            {
                Record("editor_assets", "find_asset_at");
                var memory = GetMemory(caller);
                var filter = WasmMemoryBridge.ReadString(memory, filterPtr, filterLen);
                var searchPaths = ParseNullSeparatedPaths(memory, pathsPtr, pathsLen);
                var guids = AssetDatabase.FindAssets(filter, searchPaths.ToArray());
                if (index < 0 || index >= guids.Length)
                    return -1;
                var path = AssetDatabase.GUIDToAssetPath(guids[index]);
                return WasmMemoryBridge.WriteString(memory, outPtr, maxLen, path);
            });

            Define(linker, "editor_assets", "load_text_asset", (Caller caller, int pathPtr, int pathLen, int outPtr, int maxLen) =>
            {
                Record("editor_assets", "load_text_asset");
                var memory = GetMemory(caller);
                var path = WasmMemoryBridge.ReadString(memory, pathPtr, pathLen);
                try
                {
                    var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (textAsset == null)
                        return -1;
                    return WasmMemoryBridge.WriteString(memory, outPtr, maxLen, textAsset.text);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"load_text_asset failed: {ex.Message}");
                    return -1;
                }
            });

            Define(linker, "editor_assets", "write_bulk_payload", (Caller caller, int offset, int payloadPtr, int payloadLen, int bulkType) =>
            {
                Record("editor_assets", "write_bulk_payload");
                var memory = GetMemory(caller);
                WasmMemoryBridge.WriteBulkHeader(memory, offset, (ushort)bulkType, (uint)payloadLen);
                var dest = offset + WasmMemoryBridge.BulkHeaderSize;
                if (payloadLen > 0)
                    memory.GetSpan(payloadPtr, payloadLen).CopyTo(memory.GetSpan(dest, payloadLen));
                return payloadLen + WasmMemoryBridge.BulkHeaderSize;
            });
        }

        void RegisterSceneImports(Linker linker)
        {
            Define(linker, "editor_scene", "get_object_path", (Caller caller, long handle, int outPtr, int maxLen) =>
            {
                Record("editor_scene", "get_object_path");
                if (!_objectHandles.TryGet((ulong)handle, out var obj) || obj == null)
                    return -1;

                var path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path) && obj is GameObject go)
                    path = GetHierarchyPath(go.transform);
                if (string.IsNullOrEmpty(path))
                    return 0;

                return WasmMemoryBridge.WriteString(GetMemory(caller), outPtr, maxLen, path);
            });

            Define(linker, "editor_scene", "get_serialized_property", (Caller caller, long handle, int propPtr, int propLen, int outPtr, int maxLen) =>
            {
                Record("editor_scene", "get_serialized_property");
                if (!_objectHandles.TryGet((ulong)handle, out var obj) || obj == null)
                    return -1;

                var propertyPath = WasmMemoryBridge.ReadString(GetMemory(caller), propPtr, propLen);
                var json = ReadSerializedPropertyJson(obj, propertyPath);
                if (json == null)
                    return -1;
                return WasmMemoryBridge.WriteString(GetMemory(caller), outPtr, maxLen, json);
            });

            Define(linker, "editor_scene", "get_component_count", (long handle) =>
            {
                Record("editor_scene", "get_component_count");
                if (!_objectHandles.TryGet((ulong)handle, out var obj) || obj == null)
                    return -1;

                var go = obj as GameObject ?? (obj is Component c ? c.gameObject : null);
                if (go == null)
                    return 0;
                return go.GetComponents<Component>().Length;
            });

            Define(linker, "editor_scene", "get_component_type_at", (Caller caller, long handle, int index, int outPtr, int maxLen) =>
            {
                Record("editor_scene", "get_component_type_at");
                if (!_objectHandles.TryGet((ulong)handle, out var obj) || obj == null)
                    return -1;

                var go = obj as GameObject ?? (obj is Component c ? c.gameObject : null);
                if (go == null)
                    return -1;

                var components = go.GetComponents<Component>();
                if (index < 0 || index >= components.Length)
                    return -1;

                var typeName = components[index] != null ? components[index].GetType().Name : "MissingScript";
                return WasmMemoryBridge.WriteString(GetMemory(caller), outPtr, maxLen, typeName);
            });
        }

        static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            var parts = new List<string>();
            var current = transform;
            while (current != null)
            {
                parts.Add(current.name);
                current = current.parent;
            }

            parts.Reverse();
            return string.Join("/", parts);
        }

        static string ReadSerializedPropertyJson(UnityEngine.Object obj, string propertyPath)
        {
            if (string.IsNullOrEmpty(propertyPath))
                return null;

            var so = new SerializedObject(obj);
            var prop = so.FindProperty(propertyPath);
            if (prop == null)
                return null;

            var sb = new StringBuilder();
            sb.Append("{\"type\":\"");
            sb.Append(prop.propertyType);
            sb.Append("\",\"value\":");
            AppendPropertyValueJson(sb, prop);
            sb.Append('}');
            return sb.ToString();
        }

        static void AppendPropertyValueJson(StringBuilder sb, SerializedProperty prop)
        {
            switch (prop.propertyType)
            {
                case SerializedPropertyType.String:
                    sb.Append('"').Append(EscapeJson(prop.stringValue)).Append('"');
                    break;
                case SerializedPropertyType.Integer:
                case SerializedPropertyType.Enum:
                    sb.Append(prop.intValue);
                    break;
                case SerializedPropertyType.Boolean:
                    sb.Append(prop.boolValue ? "true" : "false");
                    break;
                case SerializedPropertyType.Float:
                    sb.Append(prop.floatValue.ToString("G9"));
                    break;
                default:
                    sb.Append('"').Append(prop.propertyType.ToString()).Append('"');
                    break;
            }
        }

        static string EscapeJson(string value) =>
            (value ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");

        static List<string> ParseNullSeparatedPaths(Memory memory, int ptr, int len)
        {
            var raw = WasmMemoryBridge.ReadString(memory, ptr, len);
            var paths = new List<string>();
            foreach (var part in raw.Split('\0', StringSplitOptions.RemoveEmptyEntries))
                paths.Add(part);
            if (paths.Count == 0)
                paths.Add("Assets");
            return paths;
        }

        Memory GetMemory(Caller caller)
        {
            if (_guestMemory != null)
                return _guestMemory;

            var memory = caller.GetMemory("memory");
            if (memory != null)
            {
                _guestMemory = memory;
                return memory;
            }

            throw new InvalidOperationException("Guest module does not export linear memory.");
        }

        void Define(Linker linker, string module, string name, Action callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, Func<double> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, Func<int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, Func<long> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, Func<int, long> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, Func<long, int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, CallerAction<int, int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, CallerAction<int, int, int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, CallerAction<int, int, int, int, float> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, CallerFunc<int, int, int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, CallerFunc<long, int, int, int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, CallerFunc<int, int, int, int, int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, CallerFunc<int, int, int, int, int, int, int, int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, CallerFunc<long, int, int, int, int, int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Define(Linker linker, string module, string name, CallerFunc<long, int, int, int, int> callback)
        {
            linker.DefineFunction(module, name, callback);
            _registeredKeys.Add($"{module}.{name}");
        }

        void Record(string module, string name)
        {
            _trace.Record(module, name);
            if (HostImportVerbose.Enabled)
                Debug.Log($"[WasmEditor] host import {module}.{name}");
        }
    }
}
