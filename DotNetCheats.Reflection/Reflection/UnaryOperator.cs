﻿using System;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Reflection;

namespace Cheats.Reflection
{
    /// <summary>
    /// Represents unary operator.
    /// </summary>
    public enum UnaryOperator : int
    {
		/// <summary>
		/// A unary plus operation, such as (+a).
		/// </summary>
		Plus = ExpressionType.UnaryPlus,

		/// <summary>
		/// An arithmetic negation operation, such as (-a)
		/// </summary>
		Negate = ExpressionType.Negate,

		/// <summary>
		/// A cast or unchecked conversion operation.
		/// </summary>
		Convert = ExpressionType.Convert,

		/// <summary>
		/// A cast or checked conversion operation.
		/// </summary>
		ConvertChecked = ExpressionType.ConvertChecked,

		/// <summary>
		/// A bitwise complement or logical negation operation.
		/// </summary>
		Not = ExpressionType.Not,

		/// <summary>
		/// A ones complement operation.
		/// </summary>
		OnesComplement = ExpressionType.OnesComplement,

		/// <summary>
		/// A unary increment operation, such as (a + 1).
		/// </summary>
		Increment = ExpressionType.Increment,

		/// <summary>
		/// A unary decrement operation, such as (a - 1).
		/// </summary>
		Decrement = ExpressionType.Decrement,

		/// <summary>
		/// A type test, such as obj is T
		/// </summary>
		IsInstanceOf = ExpressionType.TypeIs,

		/// <summary>
		/// An exact type test.
		/// </summary>
		TypeTest = ExpressionType.TypeEqual,

		/// <summary>
		/// Safe typecast operation, such as obj as T
		/// </summary>
		TryConvert = ExpressionType.TypeAs,

		/// <summary>
		/// if(value)
		/// </summary>
		IsTrue = ExpressionType.IsTrue,

		/// <summary>
		/// if(!value)
		/// </summary>
		IsFalse = ExpressionType.IsFalse
	}

    /// <summary>
    /// Represents unary operator applicable to type <typeparamref name="T"/>.
    /// </summary>
	/// <typeparam name="T">Target type.</typeparam>
    /// <typeparam name="R">Type of unary operator result.</typeparam>
	[DefaultMember("Invoke")]
    public sealed class UnaryOperator<T, R> : Operator<Operator<T, R>>
    {
        private UnaryOperator(Expression<Operator<T, R>> invoker, UnaryOperator type, MethodInfo overloaded)
            : base(invoker.Compile(), type.ToExpressionType(), overloaded)
        {
        }

        /// <summary>
        /// Type of operator.
        /// </summary>
        public new UnaryOperator Type => (UnaryOperator)base.Type;

		/// <summary>
		/// Invokes unary operator.
		/// </summary>
		/// <param name="operand">An operand.</param>
		/// <returns>Result of unary operator.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public R Invoke(in T operand) => invoker(in operand);

		private static Expression<Operator<T, R>> MakeUnary(Operator.Kind @operator, Operator.Operand operand, out MethodInfo overloaded)
		{
			tail_call: //C# doesn't support tail calls so replace it with label/goto
			overloaded = null;
			try
			{
				var body = @operator.MakeUnary<R>(operand);
				overloaded = body.Method;
				return Expression.Lambda<Operator<T, R>>(body, operand.Source);
			}
			catch(ArgumentException e)
			{
				Debug.WriteLine(e);
				return null;
			}
			catch(InvalidOperationException)
			{
				//ignore exception
			}
			if(operand.Upcast())
				goto tail_call;
			else
				return null;
		}


        internal static UnaryOperator<T, R> Reflect(Operator.Kind op)
		{
			var parameter = Expression.Parameter(typeof(T).MakeByRefType(), "operand");
			var result = MakeUnary(op, parameter, out var overloaded);
            return result is null ? null : new UnaryOperator<T, R>(result, op, overloaded);
		}
    }
}