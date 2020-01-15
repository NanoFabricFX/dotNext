﻿using System;
using System.Runtime.InteropServices;

namespace DotNext.Net.Cluster.Consensus.Raft
{
    using IO;
    using IO.Log;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents No-OP entry.
    /// </summary>
    [StructLayout(LayoutKind.Auto)]
    public readonly struct EmptyEntry : IRaftLogEntry
    {
        private readonly bool isSnapshot;

        internal EmptyEntry(long term, bool snapshot)
        {
            Term = term;
            Timestamp = DateTimeOffset.UtcNow;
            isSnapshot = snapshot;
        }

        /// <summary>
        /// Initializes a new empty log entry.
        /// </summary>
        /// <param name="term">The term value.</param>
        public EmptyEntry(long term)
            : this(term, false)
        {
        }

        bool ILogEntry.IsSnapshot => isSnapshot;

        long? IDataTransferObject.Length => 0;

        bool IDataTransferObject.IsReusable => true;

        /// <summary>
        /// Gets or sets log entry term.
        /// </summary>
        public long Term { get; }

        /// <summary>
        /// Gets timestamp of this log entry.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        ValueTask IDataTransferObject.WriteToAsync<TWriter>(TWriter writer, CancellationToken token) => new ValueTask();

        ValueTask<TResult> IDataTransferObject.GetObjectDataAsync<TResult, TDecoder>(TDecoder parser, CancellationToken token) => parser.ReadAsync(IAsyncBinaryReader.Empty, token);
    }
}
