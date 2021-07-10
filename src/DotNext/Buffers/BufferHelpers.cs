using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace DotNext.Buffers
{
    /// <summary>
    /// Represents helper methods to work with various buffer representations.
    /// </summary>
    public static partial class BufferHelpers
    {
        /// <summary>
        /// Converts the sequence of memory blocks to <see cref="ReadOnlySequence{T}"/> data type.
        /// </summary>
        /// <param name="chunks">The sequence of memory blocks.</param>
        /// <typeparam name="T">The type of elements in the memory blocks.</typeparam>
        /// <returns>The constructed <see cref="ReadOnlySequence{T}"/> instance containing memory blocks.</returns>
        public static ReadOnlySequence<T> ToReadOnlySequence<T>(this IEnumerable<ReadOnlyMemory<T>> chunks)
        {
            Chunk<T>? head = null, tail = null;
            foreach (var segment in chunks)
            {
                if (!segment.IsEmpty)
                    Chunk<T>.AddChunk(segment, ref head, ref tail);
            }

            if (head is null || tail is null)
                return ReadOnlySequence<T>.Empty;

            if (ReferenceEquals(head, tail))
                return new ReadOnlySequence<T>(head.Memory);

            return Chunk<T>.CreateSequence(head, tail);
        }

        /// <summary>
        /// Converts two memory blocks to <see cref="ReadOnlySequence{T}"/> data type.
        /// </summary>
        /// <param name="first">The first memory block.</param>
        /// <param name="second">The second memory block.</param>
        /// <typeparam name="T">The type of elements in the memory blocks.</typeparam>
        /// <returns>The constructed <see cref="ReadOnlySequence{T}"/> instance containing memory blocks.</returns>
        public static ReadOnlySequence<T> Concat<T>(this ReadOnlyMemory<T> first, ReadOnlyMemory<T> second)
        {
            if (first.IsEmpty)
                return second.IsEmpty ? ReadOnlySequence<T>.Empty : new ReadOnlySequence<T>(second);

            if (second.IsEmpty)
                return new ReadOnlySequence<T>(first);

            Chunk<T>? head = null, tail = null;
            Chunk<T>.AddChunk(first, ref head, ref tail);
            Chunk<T>.AddChunk(second, ref head, ref tail);
            return Chunk<T>.CreateSequence(head, tail);
        }

        /// <summary>
        /// Constructs the string from the buffer.
        /// </summary>
        /// <param name="writer">The buffer of characters.</param>
        /// <returns>The string constructed from the buffer.</returns>
        public static string BuildString(this ArrayBufferWriter<char> writer)
        {
            var span = writer.WrittenSpan;
            return span.IsEmpty ? string.Empty : new string(span);
        }

        /// <summary>
        /// Writes single element to the buffer.
        /// </summary>
        /// <param name="writer">The buffer writer.</param>
        /// <param name="value">The value to add.</param>
        /// <typeparam name="T">The type of elements in the buffer.</typeparam>
        public static void Write<T>(this IBufferWriter<T> writer, T value)
        {
            const int count = 1;
            writer.GetSpan()[0] = value;
            writer.Advance(count);
        }

        /// <summary>
        /// Writes the sequence of elements to the buffer.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <param name="writer">The buffer writer.</param>
        /// <param name="value">The sequence of elements to be written.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write<T>(this IBufferWriter<T> writer, in ReadOnlySequence<T> value)
        {
            if (value.IsSingleSegment)
            {
                writer.Write(value.FirstSpan);
            }
            else
            {
                WriteSlow(writer, in value);
            }

            static void WriteSlow(IBufferWriter<T> writer, in ReadOnlySequence<T> value)
            {
                foreach (var segment in value)
                    writer.Write(segment.Span);
            }
        }

        /// <summary>
        /// Copies the contents from the source sequence into a destination span.
        /// </summary>
        /// <param name="source">Source sequence.</param>
        /// <param name="destination">Destination memory.</param>
        /// <param name="writtenCount">The number of copied elements.</param>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(this in ReadOnlySequence<T> source, Span<T> destination, out int writtenCount)
        {
            if (source.IsSingleSegment)
            {
                // fast path - single-segment sequence
                source.FirstSpan.CopyTo(destination, out writtenCount);
            }
            else
            {
                // slow path - multisegment sequence
                writtenCount = CopyToSlow(in source, destination);
            }

            static int CopyToSlow(in ReadOnlySequence<T> source, Span<T> destination)
            {
                int result = 0, subcount;

                for (var position = source.Start; !destination.IsEmpty && source.TryGet(ref position, out var block); result += subcount)
                {
                    block.Span.CopyTo(destination, out subcount);
                    destination = destination.Slice(subcount);
                }

                return result;
            }
        }

        /// <summary>
        /// Releases all resources encapsulated by the container.
        /// </summary>
        /// <remarks>
        /// This method calls <see cref="IDisposable.Dispose"/> for each
        /// object in the rented block.
        /// </remarks>
        /// <typeparam name="T">The type of items in the rented memory.</typeparam>
        /// <param name="owner">The rented memory.</param>
        public static void ReleaseAll<T>(this ref MemoryOwner<T> owner)
            where T : notnull, IDisposable
        {
            foreach (ref var item in owner.Memory.Span)
            {
                item.Dispose();
                item = default!;
            }

            owner.Clear(false);
        }

#if !NETSTANDARD2_1
        /// <summary>
        /// Writes the contents of string builder to the buffer.
        /// </summary>
        /// <param name="writer">The buffer writer.</param>
        /// <param name="input">The string builder.</param>
        public static void Write(this IBufferWriter<char> writer, StringBuilder input)
        {
            foreach (var chunk in input.GetChunks())
                writer.Write(chunk.Span);
        }
#endif
    }
}