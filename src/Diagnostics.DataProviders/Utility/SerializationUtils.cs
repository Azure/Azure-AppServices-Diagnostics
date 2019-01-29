using System.IO;
using System.Runtime.CompilerServices;

namespace Diagnostics.DataProviders.Utility
{
    /// <summary>
    /// Set of functions used for metrics serialization and deserialization.
    /// </summary>
    internal static class SerializationUtils
    {
        /// <summary>
        /// Reads int value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <returns>Read value.</returns>
        public static int ReadInt32FromBase128(BinaryReader reader)
        {
            return (int)ReadInt64FromBase128(reader);
        }

        /// <summary>
        /// Reads long value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64FromBase128(BinaryReader reader)
        {
            var dummy = 0;
            return ReadInt64FromBase128(reader, ref dummy);
        }

        /// <summary>
        /// Reads long value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <param name="bytesRead">The number that is incremented each time a single byte is read.</param>
        /// <returns>Read value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ReadInt64FromBase128(BinaryReader reader, ref int bytesRead)
        {
            long val = 0;
            var shift = 0;
            byte b;
            var first = true;
            var negative = false;
            do
            {
                if (first)
                {
                    first = false;
                    b = reader.ReadByte();
                    bytesRead++;
                    val += (b & 0x3f) << shift;
                    negative = (b & 0x40) != 0;
                    shift += 6;
                }
                else
                {
                    b = reader.ReadByte();
                    bytesRead++;
                    val += (long)(b & 0x7f) << shift;
                    shift += 7;
                }
            }
            while ((b & 0x80) != 0);
            return negative ? -val : val;
        }

        /// <summary>
        /// Reads uint value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <returns>Read value.</returns>
        public static uint ReadUInt32FromBase128(BinaryReader reader)
        {
            return (uint)ReadUInt64FromBase128(reader);
        }

        /// <summary>
        /// Reads ulong value stored in Base-128 encoding.
        /// </summary>
        /// <param name="reader">Binary reader to be used for reading.</param>
        /// <returns>Read value.</returns>
        public static ulong ReadUInt64FromBase128(BinaryReader reader)
        {
            ulong val = 0;
            var shift = 0;
            byte b;
            do
            {
                b = reader.ReadByte();
                val = val + ((ulong)(b & 0x7f) << shift);
                shift += 7;
            }
            while ((b & 0x80) != 0);
            return val;
        }

        /// <summary>
        /// Reads ulong value stored in Base-128 encoding.
        /// </summary>
        /// <param name="buffer">Buffer from which value to be read.</param>
        /// <param name="offset">Offset in buffer to start reading from.</param>
        /// <returns>Read value.</returns>
        public static ulong ReadUInt64FromBase128(byte[] buffer, ref int offset)
        {
            ulong val = 0;
            var shift = 0;
            byte b;
            do
            {
                b = buffer[offset++];
                val = val + ((ulong)(b & 0x7f) << shift);
                shift += 7;
            }
            while ((b & 0x80) != 0);
            return val;
        }
    }
}