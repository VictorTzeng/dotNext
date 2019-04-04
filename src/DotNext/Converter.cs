﻿using System;

namespace DotNext
{
    /// <summary>
    /// Provides extension methods for delegate <see cref="Converter{TInput, TOutput}"/> and
    /// predefined converters.
    /// </summary>
    public static class Converter
    {
        private static class Id<I, O>
            where I : O
        {
            internal static readonly Converter<I, O> Value = Identity<I, O>;
        }

        internal static O Identity<I, O>(I input) where I : O => input;

        /// <summary>
        /// The converter which returns input argument
        /// without any modifications.
        /// </summary>
        /// <typeparam name="I">Type of input.</typeparam>
        /// <typeparam name="O">Type of output.</typeparam>
        /// <returns>The identity function.</returns>
        /// <remarks>
        /// This method returns the same instance of predicate on every call.
        /// </remarks>
        public static Converter<I, O> Identity<I, O>()
            where I : O
            => Id<I, O>.Value;

        /// <summary>
        /// The converter which returns input argument
        /// without any modifications.
        /// </summary>
        /// <typeparam name="T">Type of input and output.</typeparam>
        /// <returns>The identity function.</returns>
        /// <remarks>
        /// This method returns the same instance of predicate on every call.
        /// </remarks>
        public static Converter<T, T> Identity<T>() => Identity<T, T>();

        /// <summary>
        /// Converts <see cref="Converter{I, O}"/> into <see cref="Func{I, O}"/>.
        /// </summary>
        /// <typeparam name="I">Type of input argument.</typeparam>
        /// <typeparam name="O">Return type of the function.</typeparam>
        /// <param name="converter">The converted delegate.</param>
        /// <returns>A delegate of type <see cref="Func{I, O}"/> referencing the same method as original delegate.</returns>
        public static Func<I, O> AsFunc<I, O>(this Converter<I, O> converter)
            => converter.ChangeType<Func<I, O>>();

        /// <summary>
        /// Converts <see cref="Converter{T, Boolean}"/> into predicate.
        /// </summary>
        /// <typeparam name="T">Type of predicate argument.</typeparam>
        /// <param name="converter">A delegate to convert.</param>
        /// <returns>A delegate of type <see cref="Predicate{T}"/> referencing the same method as original delegate.</returns>
        public static Predicate<T> AsPredicate<T>(this Converter<T, bool> converter)
            => converter.ChangeType<Predicate<T>>();
    }
}