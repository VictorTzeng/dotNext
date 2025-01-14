﻿using System.Collections.Generic;
using Xunit;

namespace DotNext.Collections.Generic
{
    public sealed class CollectionTests : Assert
    {
        [Fact]
        public static void ReadOnlyIndexer()
        {
            IReadOnlyList<long> array = new[] { 5L, 6L, 20L };
            Equal(20L, List.Indexer<long>.ReadOnly(array, 2));
            Equal(6L, array.IndexerGetter().Invoke(1));
        }

        [Fact]
        public static void Indexer()
        {
            IList<long> array = new[] { 5L, 6L, 30L };
            Equal(30L, List.Indexer<long>.Getter(array, 2));
            List.Indexer<long>.Setter(array, 1, 10L);
            Equal(10L, array.IndexerGetter().Invoke(1));
            array.IndexerSetter().Invoke(0, 6L);
            Equal(6L, array.IndexerGetter().Invoke(0));
        }
    }
}
