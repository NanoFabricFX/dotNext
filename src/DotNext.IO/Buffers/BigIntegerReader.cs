using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DotNext.Buffers
{
    [StructLayout(LayoutKind.Auto)]
    internal struct BigIntegerReader<TBuffer> : IBufferReader<BigInteger>
        where TBuffer : struct, IBuffer<byte>
    {
        private readonly bool littleEndian;

        // not readonly to avoid defensive copying
        private TBuffer result;
        private int length, resultOffset;

        internal BigIntegerReader(TBuffer result, bool littleEndian)
        {
            this.result = result;
            length = result.Length;
            this.littleEndian = littleEndian;
            resultOffset = 0;
        }

        public readonly int RemainingBytes => length;

        readonly BigInteger IBufferReader<BigInteger>.Complete() => new(Complete(), isBigEndian: !littleEndian);

        internal readonly Span<byte> Complete() => result.Span.Slice(0, resultOffset);

        public void Append(ReadOnlySpan<byte> bytes, ref int consumedBytes)
        {
            bytes.CopyTo(result.Span.Slice(resultOffset), out consumedBytes);
            length -= consumedBytes;
            resultOffset += consumedBytes;
        }
    }
}