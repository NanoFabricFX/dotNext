using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DotNext.Buffers
{
    using DecodingContext = Text.DecodingContext;

    [StructLayout(LayoutKind.Auto)]
    internal struct StringReader<TBuffer> : IBufferReader<string>
        where TBuffer : struct, IBuffer<char>
    {
        private readonly Decoder decoder;
        private readonly Encoding encoding;

        // not readonly to avoid defensive copying
        private TBuffer result;
        private int length, resultOffset;

        internal StringReader(in DecodingContext context, TBuffer result)
        {
            decoder = context.GetDecoder();
            encoding = context.Encoding;
            length = result.Length;
            this.result = result;
            resultOffset = 0;
        }

        public readonly int RemainingBytes => length;

        readonly string IBufferReader<string>.Complete() => new(Complete());

        internal readonly Span<char> Complete() => result.Span.Slice(0, resultOffset);

        public void Append(ReadOnlySpan<byte> bytes, ref int consumedBytes)
        {
            length -= bytes.Length;
            resultOffset += decoder.GetChars(bytes, result.Span.Slice(resultOffset), length == 0);
        }
    }
}