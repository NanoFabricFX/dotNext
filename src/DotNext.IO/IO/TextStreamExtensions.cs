using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotNext.IO
{
    using Buffers;

    /// <summary>
    /// Represents various extension methods for <see cref="TextWriter"/> and <see cref="TextReader"/> classes.
    /// </summary>
    public static class TextStreamExtensions
    {
        /// <summary>
        /// Creates text writer backed by the char buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer.</param>
        /// <param name="provider">The object that controls formatting.</param>
        /// <param name="flush">The optional implementation of <see cref="TextWriter.Flush"/> method.</param>
        /// <param name="flushAsync">The optional implementation of <see cref="TextWriter.FlushAsync"/> method.</param>
        /// <typeparam name="TWriter">The type of the char buffer writer.</typeparam>
        /// <returns>The text writer backed by the buffer writer.</returns>
        public static TextWriter AsTextWriter<TWriter>(this TWriter writer, IFormatProvider? provider = null, Action<TWriter>? flush = null, Func<TWriter, CancellationToken, Task>? flushAsync = null)
            where TWriter : class, IBufferWriter<char>
        {
            IFlushable.DiscoverFlushMethods(writer, ref flush, ref flushAsync);
            return new CharBufferWriter<TWriter>(writer, provider, flush, flushAsync);
        }

        /// <summary>
        /// Creates <see cref="TextReader"/> over the sequence of characters.
        /// </summary>
        /// <param name="sequence">The sequence of characters.</param>
        /// <returns>The reader over the sequence of characters.</returns>
        public static TextReader AsTextReader(this ReadOnlySequence<char> sequence)
            => new CharBufferReader(sequence);

        /// <summary>
        /// Creates <see cref="TextReader"/> over the sequence of encoded characters.
        /// </summary>
        /// <param name="sequence">The sequence of bytes representing encoded characters.</param>
        /// <param name="encoding">The encoding of the characters in the sequence.</param>
        /// <param name="bufferSize">The size of the internal <see cref="char"/> buffer used to decode characters.</param>
        /// <param name="allocator">The allocator of the internal buffer.</param>
        /// <returns>The reader over the sequence of encoded characters.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is <see langword="null"/>; or <paramref name="bufferSize"/> is less than or equal to zero.</exception>
        public static TextReader AsTextReader(this ReadOnlySequence<byte> sequence, Encoding encoding, int bufferSize = 1024, MemoryAllocator<char>? allocator = null)
            => new DecodingTextReader(sequence, encoding, bufferSize, allocator);

        /// <summary>
        /// Asynchronously writes a linked regions of characters to the text stream.
        /// </summary>
        /// <param name="writer">The stream to write into.</param>
        /// <param name="chars">The linked regions of characters.</param>
        /// <param name="token">The token that can be used to cancel the operation.</param>
        /// <returns>The task representing asynchronous execution of this method.</returns>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        public static async ValueTask WriteAsync(this TextWriter writer, ReadOnlySequence<char> chars, CancellationToken token = default)
        {
            foreach (var segment in chars)
                await writer.WriteAsync(segment, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Creates text writer backed by the byte buffer writer.
        /// </summary>
        /// <param name="writer">The buffer writer.</param>
        /// <param name="encoding">The encoding used to converts chars to bytes.</param>
        /// <param name="provider">The object that controls formatting.</param>
        /// <param name="flush">The optional implementation of <see cref="TextWriter.Flush"/> method.</param>
        /// <param name="flushAsync">The optional implementation of <see cref="TextWriter.FlushAsync"/> method.</param>
        /// <typeparam name="TWriter">The type of the char buffer writer.</typeparam>
        /// <returns>The text writer backed by the buffer writer.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="encoding"/> is <see langword="null"/>.</exception>
        public static TextWriter AsTextWriter<TWriter>(this TWriter writer, Encoding encoding, IFormatProvider? provider = null, Action<TWriter>? flush = null, Func<TWriter, CancellationToken, Task>? flushAsync = null)
            where TWriter : class, IBufferWriter<byte>
        {
            IFlushable.DiscoverFlushMethods(writer, ref flush, ref flushAsync);
            return new EncodingTextWriter<TWriter>(writer, encoding, provider, flush, flushAsync);
        }
    }
}