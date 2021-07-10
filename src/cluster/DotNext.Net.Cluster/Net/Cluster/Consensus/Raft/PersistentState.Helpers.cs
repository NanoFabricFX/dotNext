using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNext.Net.Cluster.Consensus.Raft
{
    public partial class PersistentState
    {
        /// <summary>
        /// Appends the entry and wait until it has been committed.
        /// </summary>
        /// <param name="entry">The entry to append.</param>
        /// <param name="timeout">The timeout to wait for the commit.</param>
        /// <param name="addToCache">
        /// <see langword="true"/> to copy the entry to in-memory cache to increase commit performance;
        /// <see langword="false"/> to avoid caching.
        /// </param>
        /// <param name="token">The token that can be used to cancel the operation.</param>
        /// <typeparam name="TEntry">The type of the log entry to be committed.</typeparam>
        /// <returns><see langword="true"/> if the entry has been committed successfully; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="OperationCanceledException">The operation has been canceled.</exception>
        /// <exception cref="TimeoutException">The operation has timed out.</exception>
        public async Task<bool> AppendAndEnsureCommitAsync<TEntry>(TEntry entry, TimeSpan timeout, bool addToCache = true, CancellationToken token = default)
            where TEntry : notnull, IRaftLogEntry
        {
            var index = await AppendAsync(entry, addToCache, token).ConfigureAwait(false);
            if (!await WaitForCommitAsync(index, timeout, token).ConfigureAwait(false))
                throw new TimeoutException();

            return entry.Term == Term;
        }
    }
}