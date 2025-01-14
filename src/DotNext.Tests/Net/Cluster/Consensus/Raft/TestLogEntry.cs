using System;

namespace DotNext.Net.Cluster.Consensus.Raft
{
    using ILogEntry = Replication.ILogEntry;
    using TextMessage = Messaging.TextMessage;

    internal sealed class TestLogEntry : TextMessage, IRaftLogEntry
    {
        public TestLogEntry(string command)
            : base(command, "Entry")
        {
            Timestamp = DateTimeOffset.UtcNow;
        }

        public DateTimeOffset Timestamp { get; }

        public long Term { get; set; }

        bool ILogEntry.IsSnapshot => false;
    }
}