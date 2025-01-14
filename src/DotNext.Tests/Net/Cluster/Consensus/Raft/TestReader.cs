﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DotNext.Net.Cluster.Consensus.Raft
{
    internal readonly struct TestReader : Replication.ILogEntryConsumer<IRaftLogEntry, DBNull>
    {
        private readonly Func<IReadOnlyList<IRaftLogEntry>, long?, ValueTask> reader;

        private TestReader(Func<IReadOnlyList<IRaftLogEntry>, long?, ValueTask> reader)
            => this.reader = reader;

        ValueTask<DBNull> Replication.ILogEntryConsumer<IRaftLogEntry, DBNull>.ReadAsync<TEntryImpl, TList>(TList entries, long? snapshotIndex, CancellationToken token)
        {
            var list = new List<IRaftLogEntry>(entries.Count);
            foreach (var entry in entries)
                list.Add(entry);
            reader(list, snapshotIndex);
            return new ValueTask<DBNull>(DBNull.Value);
        }

        public static implicit operator TestReader(Func<IReadOnlyList<IRaftLogEntry>, long?, ValueTask> reader) => new TestReader(reader);
    }
}
