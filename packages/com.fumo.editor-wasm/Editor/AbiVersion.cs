using System;

namespace Fumo.EditorWasm
{
    public static class AbiVersion
    {
        public const string Current = "editor-api/1";

        public static bool IsSupported(string abi)
        {
            if (string.IsNullOrWhiteSpace(abi))
                return false;
            return string.Equals(abi.Trim(), Current, StringComparison.Ordinal);
        }

        public static void ValidateForLoad(ToolManifest manifest)
        {
            if (manifest == null)
                throw new ArgumentNullException(nameof(manifest));

            if (!manifest.abiDeclared)
                throw new InvalidOperationException(GetMissingAbiMessage(manifest.id));

            if (!IsSupported(manifest.abi))
                throw new InvalidOperationException(GetErrorMessage(manifest.abi, manifest.id));
        }

        public static string GetMissingAbiMessage(string toolId) =>
            $"[WasmEditor] Tool '{toolId}' is missing required field 'abi'. Set \"abi\": \"{Current}\" in tool.json.";

        public static string GetErrorMessage(string abi, string toolId)
        {
            var value = string.IsNullOrWhiteSpace(abi) ? "(missing)" : abi;
            return $"[WasmEditor] Tool '{toolId}' ABI '{value}' is not supported. Required: '{Current}'.";
        }
    }
}
