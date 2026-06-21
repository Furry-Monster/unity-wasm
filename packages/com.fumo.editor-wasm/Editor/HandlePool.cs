using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Opaque handle pool for Unity objects exposed to WASM guests.
    /// </summary>
    public sealed class HandlePool<T> where T : class
    {
        readonly Dictionary<ulong, Entry> _entries = new();
        ulong _nextId = 1;

        struct Entry
        {
            public T Value;
            public bool Valid;
        }

        public ulong Register(T value)
        {
            if (value == null)
                return 0;

            var id = _nextId++;
            _entries[id] = new Entry { Value = value, Valid = true };
            return id;
        }

        public bool TryGet(ulong id, out T value)
        {
            value = null;
            if (id == 0 || !_entries.TryGetValue(id, out var entry) || !entry.Valid)
                return false;

            if (entry.Value is UnityEngine.Object unityObj && unityObj == null)
            {
                _entries[id] = new Entry { Value = entry.Value, Valid = false };
                return false;
            }

            value = entry.Value;
            return true;
        }

        public void Invalidate(ulong id)
        {
            if (_entries.TryGetValue(id, out var entry))
                _entries[id] = new Entry { Value = entry.Value, Valid = false };
        }

        public void Clear()
        {
            _entries.Clear();
            _nextId = 1;
        }

        public void Sweep()
        {
            var stale = new List<ulong>();
            foreach (var pair in _entries)
            {
                if (!pair.Value.Valid)
                    continue;

                if (pair.Value.Value is UnityEngine.Object unityObj && unityObj == null)
                    stale.Add(pair.Key);
            }

            foreach (var id in stale)
                Invalidate(id);
        }
    }
}
