﻿using System;
using Xunit;

namespace DotNext.Runtime.CompilerServices
{
    public sealed class ValueTupleBuilderTests : Assert
    {
        [Fact]
        public static void TupleTypeConstructionTest()
        {
            var builder = new ValueTupleBuilder();
            Equal(typeof(ValueTuple), builder.Build());
            builder.Add<DateTime>();
            Equal(typeof(ValueTuple<DateTime>), builder.Build());
            builder.Add<string>();
            Equal(typeof(ValueTuple<DateTime, string>), builder.Build());
            builder.Add<int>();
            Equal(typeof(ValueTuple<DateTime, string, int>), builder.Build());
            for (int i = 0; i < 16; i++)
                builder.Add<bool>();
            Equal(19, builder.Count);
            var tupleType = builder.Build();
            Equal(typeof(ValueTuple<,,,,,,,>), tupleType.GetGenericTypeDefinition());
        }
    }
}
