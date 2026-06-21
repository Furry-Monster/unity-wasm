using System;
using UnityEngine;

namespace Fumo.EditorWasm.WebView
{
    /// <summary>
    /// Phase 3 placeholder for WebView ↔ Host ↔ WASM messaging.
    /// Integrate Vuplex or another embedded browser to enable React/Vue tool UIs.
    /// </summary>
    public static class WebViewBridge
    {
        public const string ProtocolVersion = "1";

        [Serializable]
        public struct HostMessage
        {
            public string type;
            public string toolId;
            public string payloadJson;
        }

        [Serializable]
        public struct WebViewMessage
        {
            public string type;
            public string requestId;
            public string payloadJson;
        }

        /// <summary>
        /// Handle a message posted from an embedded browser panel.
        /// </summary>
        public static void HandleWebViewMessage(WebViewMessage message)
        {
            Debug.Log($"[WasmEditor][WebView] {message.type} request={message.requestId}");
            // Future: route to WasmEditorRuntime.InvokeTool or custom host APIs.
        }

        /// <summary>
        /// Push a host event to the WebView UI layer.
        /// </summary>
        public static void PostToWebView(HostMessage message)
        {
            Debug.Log($"[WasmEditor][WebView] post {message.type} tool={message.toolId}");
        }
    }
}
