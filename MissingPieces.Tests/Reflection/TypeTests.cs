﻿using System;
using System.IO;
using System.Linq.Expressions;
using Xunit;

namespace MissingPieces.Reflection
{
	public sealed class TypeTests: Assert
	{
		internal static event EventHandler StaticEvent;
		private event EventHandler InstanceEvent;

		private struct Point
		{
			public int X, Y;

			public void Zero() => X = Y = 0;
		}

		private delegate void ByRefAction<T>(in T value);

		private delegate R ByRefFunc<T1, T2, R>(in T1 value, T2 arg);

		// [Fact]
		// public void NonExistentMethodTest()
		// {
		// 	Throws<MissingMethodException>(() => Type<string>.Method.Instance<StringComparer>.Get<char>(nameof(string.IndexOf)));
		// }

		// [Fact]
		// public void InstanceMethodTest()
		// {
		// 	Func<string, char, int> indexOf = Type<string>.Method.Instance<int>.Get<char>(nameof(string.IndexOf));
		// 	var result = indexOf("aba", 'b');
		// 	Equal(1, result);

		// 	ByRefFunc<string, char, int> indexOf2 = Type<string>.Method<ByRefFunc<string, char, int>>.Instance.GetOrNull(nameof(string.IndexOf));
		// 	result = indexOf("abca", 'c');
		// 	Equal(2, result);

		// 	Func<string, char, int, int> indexOf3 = Type<string>.Method.Instance<int>.Get<char, int>(nameof(string.IndexOf));
		// 	NotNull(indexOf3);
		// 	result = indexOf3("aba", 'b', 1);
		// 	Equal(1, result);

		// 	Null(Type<Point>.Method<Action<Point>>.Instance.GetOrNull(nameof(Point.Zero)));
		// 	ByRefAction<Point> zero = Type<Point>.Method<ByRefAction<Point>>.Instance.GetOrNull(nameof(Point.Zero));
		// 	NotNull(zero);
		// 	var point = new Point() { X = 10, Y = 20 };
		// 	zero(point);
		// 	Equal(0, point.X);
		// 	Equal(0, point.Y);
		// }

		// [Fact]
		// public void StaticMethodTest()
		// {
		// 	Func<string, string, int> compare = Type<string>.Method.Static<int>.Get<string, string>(nameof(string.Compare));
		// 	NotNull(compare);
		// 	True(compare("a", "b") < 0);
		// }

		[Fact]
		public void ConstructorTests()
		{
			Func<char, int, string> stringCtor = Type<string>.Constructor<char, int>.Require();
			var str = stringCtor('a', 3);
			Equal("aaa", str);
			Func<object> objCtor = Type<object>.Constructor.Get();
			NotNull(objCtor());
			Throws<MissingConstructorException>(() => Type<string>.Constructor<int, int, string>.Require());
			Func<int, ClassWithProperties> classCtor = Type<ClassWithProperties>.Constructor<int>.Get(true);
			var obj = classCtor(10);
			Equal(10, obj.ReadOnlyProp);
		}

		[Fact]
		public void SpecialConstructorTests()
		{
			var stringCtor = Type<string>.RequireConstructor<(char, int)>();
			var str = stringCtor.Invoke(('a', 3));
			Equal("aaa", str);

			Null(Type<string>.GetConstructor<(bool, bool)>());

			var ctorWithRef = Type<ClassWithProperties>.RequireConstructor<(int first, Ref<bool> second)>();
			var args = ctorWithRef.ArgList();
			args.first = 20;
			args.second = false;
			NotNull(ctorWithRef.Invoke(args));
			True(args.second);
		}

		[Fact]
		public void ValueTypeTests()
		{
			Func<long> longCtor = Type<long>.Constructor.Get();
			Equal(0L, longCtor());
		}

		private sealed class ClassWithProperties
		{
			
			internal static long StaticProp { get; set; }

			private int value;

			public ClassWithProperties(){}

			public ClassWithProperties(int val, out bool result)
			{
				value = val;
				result = true;
			}

			internal ClassWithProperties(int val) => value = val;

			public string ReadWriteProperty { get; set; }

			public int ReadOnlyProp => value;

			public int WriteOnlyProp
			{
				set => this.value = value;
			}
		}

		private struct StructWithProperties
		{
			private int value;

			public string ReadWriteProperty { get; set; }

			public int ReadOnlyProp => value;

			public int WriteOnlyProp
			{
				set => this.value = value;
			}
		}

		[Fact]
		public void InstancePropertyTest()
		{
			var instance = new StructWithProperties();
			var rwProperty = Type<StructWithProperties>.InstanceProperty<string>.GetOrThrow(nameof(StructWithProperties.ReadWriteProperty));
			True(rwProperty.CanRead);
			True(rwProperty.CanWrite);
			NotNull(rwProperty.GetMethod);
			NotNull(rwProperty.SetMethod);
			rwProperty[instance] = "Hello, world";
			Equal("Hello, world", instance.ReadWriteProperty);
			Equal("Hello, world", rwProperty[instance]);
			var wProperty = Type<StructWithProperties>.InstanceProperty<int>.GetOrThrow(nameof(StructWithProperties.WriteOnlyProp));
			True(wProperty.CanWrite);
			False(wProperty.CanRead);
			NotNull(wProperty.SetMethod);
			Null(wProperty.GetMethod);
			wProperty[instance] = 42;
			MemberAccess<StructWithProperties, int> rProperty = Type<StructWithProperties>.InstanceProperty<int>.GetOrThrow(nameof(StructWithProperties.ReadOnlyProp));
			Equal(42, rProperty.GetValue(in instance));
		}

		[Fact]
		public void StructPropertyTest()
		{
			var instance = new StructWithProperties();
			MemberAccess<StructWithProperties, string> rwProperty = Type<StructWithProperties>.InstanceProperty<string>.GetOrThrow(nameof(StructWithProperties.ReadWriteProperty));
			rwProperty.SetValue(instance, "Hello, world");
			Equal("Hello, world", instance.ReadWriteProperty);
			Equal("Hello, world", rwProperty.GetValue(instance));
			var wProperty = Type<StructWithProperties>.InstanceProperty<int>.GetOrThrow(nameof(StructWithProperties.WriteOnlyProp));
			True(wProperty.CanWrite);
			False(wProperty.CanRead);
			NotNull(wProperty.SetMethod);
			Null(wProperty.GetMethod);
			wProperty[instance] = 42;
			var rProperty = Type<StructWithProperties>.InstanceProperty<int>.GetOrThrow(nameof(StructWithProperties.ReadOnlyProp));
			False(rProperty.CanWrite);
			True(rProperty.CanRead);
			Equal(42, rProperty[instance]);
			//Equal(42, ((MemberAccess.Getter<StructWithProperties, int>)rProperty.GetMethod).Invoke(instance));
		}

		[Fact]
		public void StaticPropertyTest()
		{
			var property = Type<ClassWithProperties>.StaticProperty<long>.GetOrThrow(nameof(ClassWithProperties.StaticProp), true);
			True(property.CanRead);
			True(property.CanWrite);
			property.Value = 42;
			Equal(42L, property.Value);
		}

		[Fact]
		public void InstanceEventTest()
		{
			var ev = Type<AppDomain>.Event<ResolveEventHandler>.Require(nameof(AppDomain.TypeResolve));
			ResolveEventHandler handler = (sender, args) => null;
			ev.AddEventHandler(AppDomain.CurrentDomain, handler);
			ev.RemoveEventHandler(AppDomain.CurrentDomain, handler);
			var ev2 = Type<TypeTests>.Event<EventHandler>.Require(nameof(InstanceEvent), true);
			Null(InstanceEvent);
			EventHandler handler2 = (sender, args) => { };
			ev2.AddEventHandler(this, handler2);
			Equal(InstanceEvent, handler2);
			ev2.RemoveEventHandler(this, handler2);
			Null(InstanceEvent);
		}

		[Fact]
		public void StaticEventTest()
		{
			var ev = Type<TypeTests>.Event<EventHandler>.RequireStatic(nameof(StaticEvent), true);
			EventHandler handler = (sender, args) => { };
			ev.AddEventHandler(handler);
			Equal(StaticEvent, handler);
			ev.RemoveEventHandler(handler);
			Null(StaticEvent);
		}

		[Fact]
		public void StaticFieldTest()
		{
			var structField = Type<Guid>.StaticField<Guid>.GetOrThrow(nameof(Guid.Empty));
			var objField = Type<TextReader>.StaticField<TextReader>.GetOrThrow(nameof(TextReader.Null));
			Same(TextReader.Null, objField.Value);
		}
	}
}