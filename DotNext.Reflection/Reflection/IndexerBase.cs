﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace DotNext.Reflection
{
	public abstract class IndexerBase<A, V>: PropertyInfo, IProperty, IEquatable<IndexerBase<A, V>>, IEquatable<PropertyInfo>
		where A: struct
	{
		private readonly PropertyInfo property;

		private protected IndexerBase(PropertyInfo property)
			=> this.property = property;

		public override object GetValue(object obj, object[] index) => property.GetValue(obj, index);

		public override void SetValue(object obj, object value, object[] index) => property.SetValue(obj, value, index);

		public sealed override string Name => property.Name;

		public sealed override bool CanRead => property.CanRead;

		public sealed override bool CanWrite => property.CanWrite;

		public sealed override MethodInfo GetMethod => property.GetMethod;

		public sealed override PropertyAttributes Attributes => property.Attributes;

		public sealed override Type PropertyType => property.PropertyType;

		public sealed override MethodInfo SetMethod => property.SetMethod;

		public sealed override MethodInfo[] GetAccessors(bool nonPublic) => property.GetAccessors(nonPublic);

		public sealed override object GetConstantValue() => property.GetConstantValue();

		public sealed override MethodInfo GetGetMethod(bool nonPublic) => property.GetGetMethod(nonPublic);

		public sealed override ParameterInfo[] GetIndexParameters() => property.GetIndexParameters();

		public sealed override Type[] GetOptionalCustomModifiers() => property.GetOptionalCustomModifiers();

		public sealed override object GetRawConstantValue() => property.GetRawConstantValue();

		public sealed override Type[] GetRequiredCustomModifiers() => property.GetRequiredCustomModifiers();

		public sealed override MethodInfo GetSetMethod(bool nonPublic) => property.GetSetMethod(nonPublic);

		public override object GetValue(object obj, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
			=> property.GetValue(obj, invokeAttr, binder, index, culture);

		public override void SetValue(object obj, object value, BindingFlags invokeAttr, Binder binder, object[] index, CultureInfo culture)
			=> property.SetValue(obj, value, invokeAttr, binder, index, culture);

		public sealed override Type DeclaringType => property.DeclaringType;

		public sealed override MemberTypes MemberType => property.MemberType;

		public sealed override Type ReflectedType => property.ReflectedType;

		public sealed override object[] GetCustomAttributes(bool inherit) => property.GetCustomAttributes(inherit);
		public sealed override object[] GetCustomAttributes(Type attributeType, bool inherit) => property.GetCustomAttributes(attributeType, inherit);

		public sealed override bool IsDefined(Type attributeType, bool inherit) => property.IsDefined(attributeType, inherit);

		public sealed override int MetadataToken => property.MetadataToken;

		public sealed override Module Module => property.Module;

		public sealed override IList<CustomAttributeData> GetCustomAttributesData() => property.GetCustomAttributesData();

		public sealed override IEnumerable<CustomAttributeData> CustomAttributes => property.CustomAttributes;

		PropertyInfo IMember<PropertyInfo>.RuntimeMember => property;

		public bool Equals(PropertyInfo other) => property == other;

		public bool Equals(IndexerBase<A, V> other)
			=> other != null &&
				GetType() == other.GetType() &&
				property == other.property;

		public override bool Equals(object other)
		{
			switch (other)
			{
				case PropertyBase<V> property:
					return Equals(property);
				case PropertyInfo property:
					return Equals(property);
				default:
					return false;
			}
		}

		public override int GetHashCode() => property.GetHashCode();

		public static bool operator ==(IndexerBase<A, V> first, IndexerBase<A, V> second) => Equals(first, second);

		public static bool operator !=(IndexerBase<A, V> first, IndexerBase<A, V> second) => !Equals(first, second);

		public override string ToString() => property.ToString();
	}

	/// <summary>
	/// Represents statix indexer property.
	/// </summary>
	/// <typeparam name="A">A structure representing parameters of indexer.</typeparam>
	/// <typeparam name="V">Property value.</typeparam>
	public sealed class Indexer<A, V>: IndexerBase<A, V>
		where A: struct
	{
		private readonly Function<A, V> getter;
		private readonly Procedure<V, A> setter;

		private Indexer(PropertyInfo property)
			: base(property)
		{
		}

		//internal static 
	}
}