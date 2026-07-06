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

        public static string GetErrorMessage(string abi, string toolId)
        {
            var value = string.IsNullOrWhiteSpace(abi) ? "(missing)" : abi;
            return $"[WasmEditor] Tool '{toolId}' ABI '{value}' is not supported. Required: '{Current}'.";
        }
    }
}
