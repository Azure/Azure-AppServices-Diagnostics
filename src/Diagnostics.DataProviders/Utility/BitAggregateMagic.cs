using System.Runtime.CompilerServices;

namespace Diagnostics.DataProviders.Utility
{
    /// <summary>
    /// The Aggregate Magic Algorithms adapted from @ http://aggregate.org/MAGIC/
    /// </summary>
    public static class BitAggregateMagic
    {
        /// <summary>
        /// The number of bits in long integer.
        /// </summary>
        public const byte NumBitsInLongInteger = 64;

        /// <summary>
        /// Counts the number of set bits in <paramref name="x"/>.
        /// </summary>
        /// <param name="x">The target for which to count the number of set bits.</param>
        /// <returns>The number of set bits in <paramref name="x"/>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CountOneBits(ulong x)
        {
            x = (x & 0x5555555555555555) + ((x >> 1) & 0x5555555555555555);
            x = (x & 0x3333333333333333) + ((x >> 2) & 0x3333333333333333);
            x = (x & 0x0f0f0f0f0f0f0f0f) + ((x >> 4) & 0x0f0f0f0f0f0f0f0f);
            x = (x & 0x00ff00ff00ff00ff) + ((x >> 8) & 0x00ff00ff00ff00ff);
            x = (x & 0x0000ffff0000ffff) + ((x >> 16) & 0x0000ffff0000ffff);
            x = (x & 0x00000000ffffffff) + ((x >> 32) & 0x00000000ffffffff);

            return (int)x;
        }
    }
}
