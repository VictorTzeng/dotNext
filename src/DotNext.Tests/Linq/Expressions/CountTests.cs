﻿using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace DotNext.Linq.Expressions
{
    public sealed class CountTests : Assert
    {
        [Fact]
        public static void StringLength()
        {
            var length = "String".Const().Count();
            IsAssignableFrom<Expression>(length);
            IsAssignableFrom<PropertyInfo>(length.Member);
            True(length.Member.DeclaringType == typeof(string));
        }

        [Fact]
        public static void ArrayLength()
        {
            var length = new string[0].Const().Count();
            IsAssignableFrom<Expression>(length);
            IsAssignableFrom<PropertyInfo>(length.Member);
            True(length.Member.DeclaringType == typeof(IReadOnlyCollection<string>));
        }

        [Fact]
        public static void ListLength()
        {
            var length = new LinkedList<string>().Const().Count();
            IsAssignableFrom<Expression>(length);
            IsAssignableFrom<PropertyInfo>(length.Member);
            True(length.Member.DeclaringType == typeof(IReadOnlyCollection<string>));
        }
    }
}
