﻿using System;
using System.Collections.Generic;

namespace DotNext.Collections.Generic
{
	/// <summary>
	/// Represents various extensions for types <see cref="Dictionary{TKey, TValue}"/>
	/// and <see cref="IDictionary{TKey, TValue}"/>.
	/// </summary>
	public static class Dictionary
	{
		/// <summary>
		/// Deconstruct key/value pair.
		/// </summary>
		/// <typeparam name="K">Type of key.</typeparam>
		/// <typeparam name="V">Type of value.</typeparam>
		/// <param name="pair">A pair to decompose.</param>
		/// <param name="key">Deconstructed key.</param>
		/// <param name="value">Deconstructed value.</param>
		public static void Deconstruct<K, V>(this KeyValuePair<K, V> pair, out K key, out V value)
		{
			key = pair.Key;
			value = pair.Value;
		}

		/// <summary>
		/// Adds a key-value pair to the dictionary if the key does not exist.
		/// </summary>
		/// <typeparam name="K">The key type of the dictionary.</typeparam>
		/// <typeparam name="V">The value type of the dictionary.</typeparam>
		/// <param name="dictionary">The source dictionary.</param>
		/// <param name="key">The key of the key-value pair.</param>
		/// <param name="value">The value of the key-value pair.</param>
		/// <returns>
		/// The corresponding value in the dictionary if <paramref name="key"/> already exists, 
		/// or <paramref name="value"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="dictionary"/> is null.
		/// </exception>
		public static V GetOrAdd<K, V>(this Dictionary<K, V> dictionary, K key, V value)
		{
			if (dictionary is null)
				throw new ArgumentNullException(nameof(dictionary));
			else if (dictionary.TryGetValue(key, out var temp))
				value = temp;
			else
				dictionary.Add(key, value);
			return value;
		}

		/// <summary>
		/// Generates a value and adds the key-value pair to the dictionary if the key does not
		/// exist.
		/// </summary>
		/// <typeparam name="K">The key type of the dictionary.</typeparam>
		/// <typeparam name="V">The value type of the dictionary.</typeparam>
		/// <param name="dictionary">The source dictionary.</param>
		/// <param name="key">The key of the key-value pair.</param>
		/// <param name="valueFactory">
		/// The function used to generate the value from the key.
		/// </param>
		/// <returns>
		/// The corresponding value in the dictionary if <paramref name="key"/> already exists, 
		/// or the value generated by <paramref name="valueFactory"/>.
		/// </returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="dictionary"/> or <paramref name="valueFactory"/> is null.
		/// </exception>
		public static V GetOrAdd<K, V>(this Dictionary<K, V> dictionary, K key, Func<K, V> valueFactory)
		{
			if (dictionary is null)
				throw new ArgumentNullException(nameof(dictionary));
			else if (valueFactory is null)
				throw new ArgumentNullException(nameof(valueFactory));
			else if (dictionary.TryGetValue(key, out var value))
				return value;
			else
			{
				value = valueFactory(key);
				dictionary.Add(key, value);
				return value;
			}
		}

		public static void ForEach<K, V>(this IDictionary<K, V> dictionary, Action<K, V> action)
		{
			foreach (var (key, value) in dictionary)
				action(key, value);
		}

		public static V GetOrInvoke<K, V>(this IDictionary<K, V> dictionary, K key, Func<V> defaultValue)
			=> dictionary.TryGetValue(key, out var value) ? value : defaultValue();

		public static Optional<T> ConvertValue<K, V, T>(this IDictionary<K, V> dictionary, K key, Converter<V, T> mapper)
			=> dictionary.TryGetValue(key, out var value) ? mapper(value) : Optional<T>.Empty;

		public static bool ConvertValue<K, V, T>(this IDictionary<K, V> dictionary, K key, Converter<V, T> mapper, out T value)
			=> dictionary.ConvertValue(key, mapper).TryGet(out value);

		/// <summary>
		/// Obtains read-only view of the dictionary.
		/// </summary>
		/// <remarks>
		/// Any changes in the dictionary will be visible from read-only view.
		/// </remarks>
		/// <typeparam name="K">Type of keys.</typeparam>
		/// <typeparam name="V">Type of values.</typeparam>
		/// <param name="dictionary">A dictionary.</param>
		/// <returns>Read-only view of the dictionary.</returns>
		public static ReadOnlyDictionaryView<K, V> AsReadOnlyView<K, V>(this IDictionary<K, V> dictionary)
			=> new ReadOnlyDictionaryView<K, V>(dictionary);

		/// <summary>
		/// Applies lazy conversion for each dictionary value.
		/// </summary>
		/// <typeparam name="K">Type of keys.</typeparam>
		/// <typeparam name="V">Type of values.</typeparam>
		/// <typeparam name="T">Type of mapped values.</typeparam>
		/// <param name="dictionary">A dictionary to be mapped.</param>
		/// <param name="mapper">Mapping function.</param>
		/// <returns>Read-only view of the dictionary where each value is converted in lazy manner.</returns>
		public static ReadOnlyDictionaryView<K, V, T> Convert<K, V, T>(this IReadOnlyDictionary<K, V> dictionary, Converter<V, T> mapper)
			=> new ReadOnlyDictionaryView<K, V, T>(dictionary, mapper);
	}
}