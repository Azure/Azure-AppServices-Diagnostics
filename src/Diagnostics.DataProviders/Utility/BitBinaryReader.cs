using System.IO;
using System.Runtime.CompilerServices;

namespace Diagnostics.DataProviders.Utility
{
    /// <summary>
    /// The class which allows reading bits from stream one by one.
    /// </summary>
    public sealed class BitBinaryReader
    {
        private const int HighestBitInByte = 1 << 7;
        private readonly BinaryReader reader;
        private int currentBit;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitBinaryReader"/> class.
        /// </summary>
        /// <param name="reader">The reader.</param>
        public BitBinaryReader(BinaryReader reader)
        {
            this.reader = reader;
            this.currentBit = 0;
        }

        /// <summary>
        /// Gets the value of the currently byte.
        /// </summary>
        public byte CurrentByte { get; private set; }

        /// <summary>
        /// Gets the <see cref="BinaryReader"/>.
        /// </summary>
        public BinaryReader BinaryReader => this.reader;

        /// <summary>
        /// Reads bits from the stream.
        /// </summary>
        /// <param name="numBits">The number of bits.</param>
        /// <returns>
        /// Read bit.
        /// </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public long ReadBits(int numBits)
        {
            long result = 0;
            for (int i = numBits - 1; i >= 0; --i)
            {
                if (this.currentBit == 0)
                {
                    this.CurrentByte = this.reader.ReadByte();
                    this.currentBit = HighestBitInByte;
                }

                var bitSet = (this.CurrentByte & this.currentBit) != 0;

                this.currentBit >>= 1;

                if (bitSet)
                {
                    result |= 1L << i;
                }
            }

            return result;
        }

        /// <summary>
        /// Reads one bit from the stream.
        /// </summary>
        /// <returns>Read bit.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ReadBit()
        {
            if (this.currentBit == 0)
            {
                this.CurrentByte = this.reader.ReadByte();
                this.currentBit = HighestBitInByte;
            }

            var result = (this.CurrentByte & this.currentBit) != 0;

            this.currentBit >>= 1;

            return result;
        }
    }
}
