using System;
using System.Buffers.Binary;
using System.Text;
using Wasmtime;

namespace Fumo.EditorWasm
{
    /// <summary>
    /// Helpers for reading/writing guest linear memory.
    /// Bulk header: magic u32 (0x464D4F42 "FMBO"), version u16, type u16, payload_len u32.
    /// </summary>
    public static class WasmMemoryBridge
    {
        public const uint BulkMagic = 0x464D4F42; // "FMBO"
        public const int BulkHeaderSize = 12;

        public static string ReadString(Memory memory, int ptr, int len)
        {
            ValidateBounds(memory, ptr, len);
            return memory.ReadString(ptr, len);
        }

        public static int WriteString(Memory memory, int ptr, int maxLen, string value)
        {
            if (value == null)
                value = string.Empty;

            var bytes = Encoding.UTF8.GetBytes(value);
            if (bytes.Length > maxLen)
                bytes = bytes.AsSpan(0, maxLen).ToArray();

            ValidateBounds(memory, ptr, bytes.Length);
            bytes.CopyTo(memory.GetSpan(ptr, bytes.Length));
            return bytes.Length;
        }

        public static void WriteBulkHeader(Memory memory, int offset, ushort type, uint payloadLen)
        {
            ValidateBounds(memory, offset, BulkHeaderSize);
            var span = memory.GetSpan(offset, BulkHeaderSize);
            BinaryPrimitives.WriteUInt32LittleEndian(span, BulkMagic);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(4), 1);
            BinaryPrimitives.WriteUInt16LittleEndian(span.Slice(6), type);
            BinaryPrimitives.WriteUInt32LittleEndian(span.Slice(8), payloadLen);
        }

        public static bool TryReadBulkHeader(Memory memory, int offset, out ushort type, out uint payloadLen)
        {
            type = 0;
            payloadLen = 0;
            if (offset < 0 || offset + BulkHeaderSize > memory.GetLength())
                return false;

            var span = memory.GetSpan(offset, BulkHeaderSize);
            if (BinaryPrimitives.ReadUInt32LittleEndian(span) != BulkMagic)
                return false;

            var version = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(4));
            if (version != 1)
                return false;

            type = BinaryPrimitives.ReadUInt16LittleEndian(span.Slice(6));
            payloadLen = BinaryPrimitives.ReadUInt32LittleEndian(span.Slice(8));
            return true;
        }

        static void ValidateBounds(Memory memory, int ptr, int len)
        {
            if (ptr < 0 || len < 0 || ptr + (long)len > memory.GetLength())
                throw new ArgumentOutOfRangeException(nameof(ptr), "Guest memory access out of bounds.");
        }
    }
}
