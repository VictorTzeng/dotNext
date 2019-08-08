Value Delegates
====
Value Delegates provide general-purpose, CLS compliant, allocation-free, lightweight callback capability to .NET languages. They can be used as regular .NET delegates but have different properties:
1. Value Delegate is a value type and don't require heap allocation
1. Multicast is not supported
1. Value Delegate cannot be used for declaration of events
1. It is not possible to declare custom Value Delegate

.NEXT provides ready-to-use set of Value Delegates:
1. [ValueAction](../../api/DotNext.ValueAction.yml) as alternative to [Action](https://docs.microsoft.com/en-us/dotnet/api/system.action)
1. [ValueAction&lt;T&gt;](../../api/DotNext.ValueAction-1.yml) as alternative to [Action&lt;T&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.action-1)
1. [ValueAction&lt;T1,T2&gt;](../../api/DotNext.ValueAction-2.yml) as alternative to [Action&lt;T1,T2&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.action-2)
1. [ValueAction&lt;T1,T2,T3&gt;](../../api/DotNext.ValueAction-3.yml) as alternative to [Action&lt;T1,T2,T3&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.action-3)
1. [ValueAction&lt;T1,T2,T3,T4&gt;](../../api/DotNext.ValueAction-4.yml) as alternative to [Action&lt;T1,T2,T3,T4&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.action-4)
1. [ValueAction&lt;T1,T2,T3,T4,T5&gt;](../../api/DotNext.ValueAction-5.yml) as alternative to [Action&lt;T1,T2,T3,T4&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.action-5)
1. [ValueFunc&lt;R&gt;](../../api/DotNext.ValueFunc-1.yml) as alternative to [Func&lt;R&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.func-1)
1. [ValueFunc&lt;T,R&gt;](../../api/DotNext.ValueFunc-2.yml) as alternative to [Func&lt;T,R&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.func-2)
1. [ValueFunc&lt;T1,T2,R&gt;](../../api/DotNext.ValueFunc-3.yml) as alternative to [Func&lt;T1,T2,R&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.func-3)
1. [ValueFunc&lt;T1,T2,T3,R&gt;](../../api/DotNext.ValueFunc-4.yml) as alternative to [Func&lt;T1,T2,T3,R&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.func-4)
1. [ValueFunc&lt;T1,T2,T3,T4,R&gt;](../../api/DotNext.ValueFunc-5.yml) as alternative to [Func&lt;T1,T2,T3,T4,R&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.func-5)
1. [ValueFunc&lt;T1,T2,T3,T4,T5,R&gt;](../../api/DotNext.ValueFunc-6.yml) as alternative to [Func&lt;T1,T2,T3,T4,T5,R&gt;](https://docs.microsoft.com/en-us/dotnet/api/system.func-6)

> [!NOTE]
> It is recommended to pass Value Delegates by reference using **in** modifier to avoid copying on the stack. 

Value Delegates are convertible with regular delegates in both directions:
```csharp
using DotNext;
using System;

Func<string, int> parse = int.Parse;
//from regular to Value Delegate
ValueFunc<string, int> valueFunc = new ValueFunc<string, int>(parse);
//from Value Delegate to regular
parse = valueFunc.ToDelegate();
```

Internally, Value Delegate is operating in two modes:
1. _Normal_ mode means that the delegate just holds the pointer to the managed method for subsequent invocations. This mode is enabled only if the method passed into Value Delegate is static or instance without implicitly captured first argument (or **this** object). This is the most profitable mode because the delegate doesn't have any references to the heap.
1. _Proxy_ mode means that the delegate acts as a wrapper for regular .NET delegate. This mode is enabled if the method passed into Value Delegate is abstract or have implicitly captured object. The delegate holds a reference to the regular .NET delegate allocated on the heap.

However, _proxy_ mode is not useless and allow to achieve uniformity across API, utilizing both types of delegates:
```csharp
using DotNext;
using System;

public static class ArrayUtils
{
	public static void Sort<T>(T[] array, in ValueFunc<T, T, int> comparer)
	{
		//sorting algorithm
	}

	public static void Sort<T>(T[] array, Func<T, T, int> comparer)
		=> Sort(array, new ValueFunc<T, T, int>(comparer, true));
}
```
The second argument of [ValueFunc&lt;T1,T2,R&gt;](../../api/DotNext.ValueFunc-3.yml) constructor which is `true` means that it should be created in _proxy_ mode. 

C# language has first-class support of .NET regular delegates in the form of the special syntax where method group can be passed into delegate constructor. Value Delegate doesn't have such native compiler support. Therefore, it uses regular .NET delegates for instantiation. If underlying delegate represents static or instance methods, its [Target](https://docs.microsoft.com/en-us/dotnet/api/system.delegate.target) property is **null* then Value Delegate will be created in _normal_ mode. Obviously, the construction of Value Delegate requires memory allocation in the form of regular delegate. However, the created delegate can be reclaimed by GC immediately after instantiation of Value Delegate. Therefore, it is recommended to save the created Value Delegate into `static readonly` as reuse it whenever possible.
```csharp
using DotNext;
using System;

public static class ComparisonUtils
{
	public static readonly ValueFunc<long, long, int> ComparerInt64 = new ValueFunc<long, long, int>(CompareInt64);

	private static int CompareInt64(long x, long y)
	{
		if(x < y)
			return -1;
		if(x > y)
			return 1;
		return 0;
	}
}
```

The second `bool` parameter of Value Deleate constructor is not used because _proxy_ mode should not be forced. The parameter is just a hint and even if it not specified or specified as `false` then Value Delegate can be constructed as _proxy_ if passed delegate instance doesn't satisfy to the requirements expected by _normal_ mode.

> [!WARNING]
> It is not recommended to create Value Delegates using lambda expression because C# compiler produces hidden closure even if nothing is captured from outer lexical scope. This closure is stored in _Target_ property of the delegate and force _proxy_ mode.

Invocation of Value Delegate has approximately the same performance as regular .NET delegates. To verify that, check out [Benchmarks](../../benchmarks.md).