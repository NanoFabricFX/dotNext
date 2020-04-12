using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DotNext.Net.Cluster.Consensus.Raft.TransportServices
{
    using IO.Log;

    internal interface IRaftRpcHandler
    {
        Task<Result<bool>> ReceiveEntriesAsync<TEntry>(EndPoint sender, long senderTerm, ILogEntryProducer<TEntry> entries, long prevLogIndex, long prevLogTerm, long commitIndex, CancellationToken token)
            where TEntry : IRaftLogEntry;

        Task<Result<bool>> ReceiveVoteAsync(EndPoint sender, long term, long lastLogIndex, long lastLogTerm, CancellationToken token);

        Task<bool> ResignAsync(CancellationToken token);

        Task<Result<bool>> ReceiveSnapshotAsync<TSnapshot>(EndPoint sender, long senderTerm, TSnapshot snapshot, long snapshotIndex, CancellationToken token)
            where TSnapshot : IRaftLogEntry;
    }

    internal interface ILocalMember : IRaftRpcHandler
    {
        IReadOnlyDictionary<string, string> Metadata { get; }
    }
}