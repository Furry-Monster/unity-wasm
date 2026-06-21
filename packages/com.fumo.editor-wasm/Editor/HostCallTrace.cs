using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Records recent host import calls for trap diagnostics and AI agents.
    /// </summary>
    public sealed class HostCallTrace
    {
        const int MaxEntries = 64;
        readonly Queue<string> _entries = new();
        readonly object _lock = new();

        public void Record(string module, string name)
        {
            var entry = $"{module}.{name}";
            lock (_lock)
            {
                _entries.Enqueue(entry);
                while (_entries.Count > MaxEntries)
                    _entries.Dequeue();
            }
        }

        public IReadOnlyList<string> Snapshot()
        {
            lock (_lock)
                return _entries.ToArray();
        }

        public void Clear()
        {
            lock (_lock)
                _entries.Clear();
        }
    }

    [Serializable]
    public sealed class TrapReport
    {
        public string toolId;
        public string trapMessage;
        public string trapCode;
        public string wasmFunc;
        public TrapFrameReport[] stack;
        public ulong fuelRemaining;
        public string[] hostCallTrace;
        public string timestampUtc;

        public string ToJson()
        {
            return UnityEngine.JsonUtility.ToJson(this, prettyPrint: true);
        }

        public static TrapReport FromException(string toolId, Wasmtime.TrapException ex, ulong fuelRemaining, HostCallTrace trace)
        {
            var frames = ex.Frames;
            var stack = new TrapFrameReport[frames?.Count ?? 0];
            if (frames != null)
            {
                for (var i = 0; i < frames.Count; i++)
                {
                    stack[i] = new TrapFrameReport
                    {
                        func = frames[i].FunctionName ?? "<unknown>",
                        module = frames[i].ModuleName ?? "<unknown>"
                    };
                }
            }

            return new TrapReport
            {
                toolId = toolId,
                trapMessage = ex.Message,
                trapCode = ex.Type.ToString(),
                wasmFunc = stack.Length > 0 ? stack[0].func : null,
                stack = stack,
                fuelRemaining = fuelRemaining,
                hostCallTrace = trace.Snapshot() is { Count: > 0 } t ? t.ToArray() : Array.Empty<string>(),
                timestampUtc = DateTime.UtcNow.ToString("o")
            };
        }
    }

    [Serializable]
    public struct TrapFrameReport
    {
        public string func;
        public string module;
    }
}
