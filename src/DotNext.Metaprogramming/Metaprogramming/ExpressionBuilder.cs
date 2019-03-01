﻿using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace DotNext.Metaprogramming
{
    using static Threading.AtomicLong;

    /// <summary>
    /// Represents basic lexical scope support.
    /// </summary>
    public abstract class ExpressionBuilder: Disposable
    {
        private readonly IDictionary<string, ParameterExpression> variables;
        private readonly ICollection<Expression> statements;
        private long nameGenerator;

        private protected ExpressionBuilder(ExpressionBuilder parent = null)
        {
            Parent = parent;
            variables = new Dictionary<string, ParameterExpression>();
            statements = new LinkedList<Expression>();
        }

        private protected B FindScope<B>()
            where B: ExpressionBuilder
        {
            for (var current = this; !(current is null); current = current.Parent)
                if (current is B scope)
                    return scope;
            return null;
        }

        internal string NextName(string prefix) => Parent is null ? prefix + nameGenerator.IncrementAndGet() : Parent.NextName(prefix);

        /// <summary>
        /// Sets body of this scope as single expression.
        /// </summary>
        public virtual Expression Body
        {
            set
            {
                variables.Clear();
                statements.Clear();
                statements.Add(value);
            }
        }

        /// <summary>
        /// Represents parent scope.
        /// </summary>
        public ExpressionBuilder Parent{ get; }

        internal E AddStatement<E>(E expression)
            where E: Expression
        {
            statements.Add(expression);
            return expression;
        }

        internal static E Build<E, B>(B builder, Action<B> body)
            where E: Expression
            where B: IExpressionBuilder<E>
        {
            body(builder);
            return builder.Build();
        }

        private E AddStatement<E, B>(B builder, Action<B> body)
            where E: Expression
            where B: IExpressionBuilder<E>
            => AddStatement(Build<E, B>(builder, body));

        /// <summary>
        /// Adds no-operation instruction to this scope.
        /// </summary>
        /// <returns>No-operation instruction.</returns>
        public Expression Nop() => AddStatement(Expression.Empty());
        
        /// <summary>
        /// Adds assignment operation to this scope.
        /// </summary>
        /// <param name="variable">The variable to modify.</param>
        /// <param name="value">The value to be assigned to the variable.</param>
        /// <returns>Assign operation.</returns>
        public BinaryExpression Assign(ParameterExpression variable, UniversalExpression value)
            => AddStatement(Expression.Assign(variable, value));

        /// <summary>
        /// Adds local variable assignment operation this scope.
        /// </summary>
        /// <param name="variableName">The name of the declared local variable.</param>
        /// <param name="value">The value to be assigned to the local variable.</param>
        public void Assign(string variableName, UniversalExpression value)
            => Assign(this[variableName], value);
        
        /// <summary>
        /// Adds instance property assignment.
        /// </summary>
        /// <param name="instance"><see langword="this"/> argument.</param>
        /// <param name="instanceProperty">Instance property to be assigned.</param>
        /// <param name="value">A new value of the property.</param>
        public void Assign(Expression instance, PropertyInfo instanceProperty, UniversalExpression value)
            => AddStatement(Expression.Assign(Expression.Property(instance, instanceProperty), value));
        
        /// <summary>
        /// Adds static property assignment.
        /// </summary>
        /// <param name="staticProperty">Static property to be assigned.</param>
        /// <param name="value">A new value of the property.</param>
        public void Assign(PropertyInfo staticProperty, UniversalExpression value)
            => Assign(null, staticProperty, value);

        /// <summary>
        /// Adds instance field assignment.
        /// </summary>
        /// <param name="instance"><see langword="this"/> argument.</param>
        /// <param name="instanceField">Instance field to be assigned.</param>
        /// <param name="value">A new value of the field.</param>
        public void Assign(Expression instance, FieldInfo instanceField, UniversalExpression value)
            => AddStatement(Expression.Assign(Expression.Field(instance, instanceField), value));

        /// <summary>
        /// Adds static field assignment.
        /// </summary>
        /// <param name="staticField">Static field to be assigned.</param>
        /// <param name="value">A new value of the field.</param>
        public void Assign(FieldInfo staticField, UniversalExpression value)
            => Assign(null, staticField, value);

        /// <summary>
        /// Adds invocation statement.
        /// </summary>
        /// <param name="delegate">The expression providing delegate to be invoked.</param>
        /// <param name="arguments">Delegate invocation arguments.</param>
        /// <returns>Invocation expression.</returns>
        public InvocationExpression Invoke(UniversalExpression @delegate, IEnumerable<Expression> arguments)
            => AddStatement(Expression.Invoke(@delegate, arguments));

        /// <summary>
        /// Adds invocation statement.
        /// </summary>
        /// <param name="delegate">The expression providing delegate to be invoked.</param>
        /// <param name="arguments">Delegate invocation arguments.</param>
        /// <returns>Invocation expression.</returns>
        public InvocationExpression Invoke(UniversalExpression @delegate, params UniversalExpression[] arguments)
            => AddStatement(@delegate.Invoke(arguments));

        /// <summary>
        /// Adds instance method call statement.
        /// </summary>
        /// <param name="instance"><see langword="this"/> argument.</param>
        /// <param name="method">The method to be called.</param>
        /// <param name="arguments">Method call arguments.</param>
        /// <returns>Method call statement.</returns>
        public MethodCallExpression Call(UniversalExpression instance, MethodInfo method, IEnumerable<Expression> arguments)
            => AddStatement(Expression.Call(instance, method, arguments));

        /// <summary>
        /// Adds instance method call statement.
        /// </summary>
        /// <param name="instance"><see langword="this"/> argument.</param>
        /// <param name="method">The method to be called.</param>
        /// <param name="arguments">Method call arguments.</param>
        /// <returns>Method call statement.</returns>
        public MethodCallExpression Call(UniversalExpression instance, MethodInfo method, params UniversalExpression[] arguments)
            => Call(instance, method, UniversalExpression.AsExpressions((IEnumerable<UniversalExpression>)arguments));

        /// <summary>
        /// Adds static method call statement.,
        /// </summary>
        /// <param name="method">The method to be called.</param>
        /// <param name="arguments">Method call arguments.</param>
        /// <returns>Method call statement.</returns>
        public MethodCallExpression Call(MethodInfo method, IEnumerable<Expression> arguments)
            => AddStatement(Expression.Call(null, method, arguments));

        /// <summary>
        /// Adds static method call statement.
        /// </summary>
        /// <param name="method">The method to be called.</param>
        /// <param name="arguments">Method call arguments.</param>
        /// <returns>Method call statement.</returns>
        public MethodCallExpression Call(MethodInfo method, params UniversalExpression[] arguments)
            => Call(method, UniversalExpression.AsExpressions((IEnumerable<UniversalExpression>)arguments));
        
        /// <summary>
        /// Declares label of the specified type.
        /// </summary>
        /// <param name="type">The type of landing site.</param>
        /// <param name="name">The optional name of the label.</param>
        /// <returns>Declared label.</returns>
        public LabelTarget Label(Type type, string name = null)
        {
            var target = Expression.Label(type, name);
            Label(target);
            return target;
        }

        /// <summary>
        /// Declares label of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of landing site.</typeparam>
        /// <param name="name">The optional name of the label.</param>
        /// <returns>Declared label.</returns>
        public LabelTarget Label<T>(string name = null) => Label(typeof(T), name);

        /// <summary>
        /// Declares label in the current scope.
        /// </summary>
        /// <returns>Declared label.</returns>
        public LabelTarget Label() => Label(typeof(void));

        /// <summary>
        /// Adds label landing site to this scope.
        /// </summary>
        /// <param name="target">The label target.</param>
        /// <returns>The landing site for the label.</returns>
        public LabelExpression Label(LabelTarget target)
            => AddStatement(Expression.Label(target));

        /// <summary>
        /// Adds unconditional control transfer statement to this scope.
        /// </summary>
        /// <param name="target">The label reference.</param>
        /// <param name="value">The value to be associated with the control transfer.</param>
        /// <returns>Unconditional control transfer statement.</returns>
        public GotoExpression Goto(LabelTarget target, UniversalExpression value)
            => AddStatement(Expression.Goto(target, value));

        /// <summary>
        /// Adds unconditional control transfer statement to this scope.
        /// </summary>
        /// <param name="target">The label reference.</param>
        /// <returns>Unconditional control transfer statement.</returns>
        public GotoExpression Goto(LabelTarget target) => Goto(target, default);

        private bool HasVariable(string name) => variables.ContainsKey(name) || Parent != null && Parent.HasVariable(name);
        
        /// <summary>
        /// Gets declared local variable in the current or parent scope.
        /// </summary>
        /// <param name="localVariableName">The name of the local variable.</param>
        /// <returns>Declared local variable; or <see langword="null"/>, if there is no declared local variable with the given name.</returns>
        public ParameterExpression this[string localVariableName]
            => variables.TryGetValue(localVariableName, out var variable) ? variable : Parent?[localVariableName];

        private protected void DeclareVariable(ParameterExpression variable)
            => variables.Add(variable.Name, variable);

        /// <summary>
        /// Declares local variable in the current lexical scope.
        /// </summary>
        /// <typeparam name="T">The type of local variable.</typeparam>
        /// <param name="name">The name of local variable.</param>
        /// <returns>The expression representing local variable.</returns>
        public ParameterExpression DeclareVariable<T>(string name)
            => DeclareVariable(typeof(T), name);

        /// <summary>
        /// Declares and initializes local variable in the current lexical scope.
        /// </summary>
        /// <typeparam name="T">The type of local variable.</typeparam>
        /// <param name="name">The name of local variable.</param>
        /// <param name="initialValue">The initial value of the local variable.</param>
        /// <returns>The expression representing local variable.</returns>
        public ParameterExpression DeclareVariable<T>(string name, T initialValue)
        {
            var variable = DeclareVariable<T>(name);
            Assign(variable, Expression.Constant(initialValue, typeof(T)));
            return variable;
        }

        /// <summary>
        /// Declares local variable in the current lexical scope. 
        /// </summary>
        /// <param name="variableType">The type of local variable.</param>
        /// <param name="name">The name of local variable.</param>
        /// <returns>The expression representing local variable.</returns>
        public ParameterExpression DeclareVariable(Type variableType, string name)
        {
            var variable = Expression.Variable(variableType, name);
            variables.Add(name, variable);
            return variable;
        }

        /// <summary>
        /// Adds if-then-else statement to this scope.
        /// </summary>
        /// <param name="test">Test expression.</param>
        /// <returns>Conditional statement builder.</returns>
        public ConditionalBuilder If(UniversalExpression test)
            => new ConditionalBuilder(test, this, true);

        /// <summary>
        /// Adds if-then statement to this scope.
        /// </summary>
        /// <param name="test">Test expression.</param>
        /// <param name="ifTrue">Positive branch builder.</param>
        /// <returns>Constructed statement.</returns>
        public ConditionalExpression IfThen(UniversalExpression test, Action<ExpressionBuilder> ifTrue)
            => If(test).Then(ifTrue).End();

        /// <summary>
        /// Adds if-then-else statement to this scope.
        /// </summary>
        /// <param name="test">Test expression.</param>
        /// <param name="ifTrue">Positive branch builder.</param>
        /// <param name="ifFalse">Negative branch builder.</param>
        /// <returns>Constructed statement.</returns>
        public ConditionalExpression IfThenElse(UniversalExpression test, Action<ExpressionBuilder> ifTrue, Action<ExpressionBuilder> ifFalse)
            => If(test).Then(ifTrue).Else(ifFalse).End();

        /// <summary>
        /// Adds <see langword="while"/> loop statement.
        /// </summary>
        /// <param name="test">Loop continuation condition.</param>
        /// <param name="loop">Loop body.</param>
        /// <returns>Loop statement.</returns>
        public LoopExpression While(UniversalExpression test, Action<WhileLoopBuider> loop)
            => AddStatement<LoopExpression, WhileLoopBuider>(new WhileLoopBuider(test, this, true), loop);

        /// <summary>
        /// Adds <code>do{ } while(condition);</code> loop statement.
        /// </summary>
        /// <param name="test">Loop continuation condition.</param>
        /// <param name="loop">Loop body.</param>
        /// <returns>Loop statement.</returns>
        public LoopExpression DoWhile(UniversalExpression test, Action<WhileLoopBuider> loop)
            => AddStatement<LoopExpression, WhileLoopBuider>(new WhileLoopBuider(test, this, false), loop);

        /// <summary>
        /// Adds <see langword="foreach"/> loop statement.
        /// </summary>
        /// <param name="collection">The expression providing enumerable collection.</param>
        /// <param name="loop">Loop body.</param>
        /// <returns>Loop statement.</returns>
        /// <seealso cref="ForEachLoopBuilder"/>
        public TryExpression ForEach(UniversalExpression collection, Action<ForEachLoopBuilder> loop)
            => AddStatement<TryExpression, ForEachLoopBuilder>(new ForEachLoopBuilder(collection, this), loop);

        /// <summary>
        /// Adds <see langword="for"/> loop statement.
        /// </summary>
        /// <remarks>
        /// This builder constructs the statement equivalent to <code>for(var i = initializer; condition; iter){ body; }</code>
        /// </remarks>
        /// <param name="initializer">Loop variable initialization expression.</param>
        /// <param name="condition">Loop continuation condition.</param>
        /// <param name="loop">Loop body.</param>
        /// <returns>Loop statement.</returns>
        /// <seealso cref="ForLoopBuilder"/>
        public LoopExpression For(UniversalExpression initializer, Func<UniversalExpression, Expression> condition, Action<ForLoopBuilder> loop)
            => AddStatement<LoopExpression, ForLoopBuilder>(new ForLoopBuilder(initializer, condition, this), loop);
        
        /// <summary>
        /// Adds generic loop statement.
        /// </summary>
        /// <param name="loop">Loop body.</param>
        /// <returns>Loop statement.</returns>
        /// <seealso cref="LoopBuilder"/>
        public LoopExpression Loop(Action<LoopBuilder> loop)
            => AddStatement<LoopExpression, LoopBuilder>(new LoopBuilder(this), loop);

        
        public LambdaExpression Lambda<D>(Action<LambdaBuilder<D>> lambda)
            where D: Delegate
            => AddStatement<LambdaExpression, LambdaBuilder<D>>(new LambdaBuilder<D>(this), lambda);

        public LambdaExpression AsyncLambda<D>(Action<AsyncLambdaBuilder<D>> lambda)
            where D : Delegate
            => AddStatement<LambdaExpression, AsyncLambdaBuilder<D>>(new AsyncLambdaBuilder<D>(this), lambda);

        public TryBuilder Try(UniversalExpression body) => new TryBuilder(body, this, true);

        public TryBuilder Try(Action<ScopeBuilder> scope) => Try(Scope(scope));

        public UnaryExpression Throw(UniversalExpression exception)
            => AddStatement(Expression.Throw(exception));

        public UnaryExpression Throw<E>()
            where E : Exception, new()
            => Throw(Expression.New(typeof(E).GetConstructor(Array.Empty<Type>())));

        public Expression Scope(Action<ScopeBuilder> scope)
            => new ScopeBuilder(this).Build(scope);

        public Expression With(UniversalExpression expression, Action<WithBlockBuilder> scope)
            => AddStatement<Expression, WithBlockBuilder>(new WithBlockBuilder(expression, this), scope);

        public Expression Using(UniversalExpression expression, Action<UsingBlockBuilder> scope)
            => AddStatement<Expression, UsingBlockBuilder>(new UsingBlockBuilder(expression, this), scope);

        public SwitchBuilder Switch(UniversalExpression switchValue)
            => new SwitchBuilder(switchValue, this, true);

        /// <summary>
        /// Constructs <see langword="return"/> instruction to return from
        /// underlying lambda function having <see langword="void"/>
        /// return type.
        /// </summary>
        /// <returns><see langword="return"/> instruction.</returns>
        public abstract Expression Return();

        /// <summary>
        /// Constructs <see langword="return"/> instruction to return from
        /// underlying lambda function having non-<see langword="void"/>
        /// return type.
        /// </summary>
        /// <param name="result">The value to be returned from the lambda function.</param>
        /// <returns><see langword="return"/> instruction.</returns>
        public abstract Expression Return(UniversalExpression result);

        internal virtual Expression Build()
        {
            switch(statements.Count)
            {
                case 0:
                    return Expression.Empty();
                case 1:
                    if(variables.Count == 0 && statements.Count == 1)
                        return statements.First();
                    else
                        goto default;
                default:
                    return Expression.Block(variables.Values, statements);
            }
        }

        /// <summary>
        /// Releases all resources associated with this builder.
        /// </summary>
        /// <param name="disposing"><see langword="true"/>, if this method is called from <see cref="Disposable.Dispose()"/>; <see langword="false"/> if called from finalizer.</param>
        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                variables.Clear();
                statements.Clear();
            }
        }
    }

    public abstract class ExpressionBuilder<E> : IExpressionBuilder<E>
        where E : Expression
    {
        private readonly ExpressionBuilder parent;
        private readonly bool treatAsStatement;
        private Type expressionType;

        private protected ExpressionBuilder(ExpressionBuilder parent, bool treatAsStatement)
        {
            this.parent = parent;
            this.treatAsStatement = treatAsStatement;
        }

        private protected ScopeBuilder NewScope() => new ScopeBuilder(parent);

        private protected B NewScope<B>(Func<ExpressionBuilder, B> factory) 
            where B: ScopeBuilder
            => factory(parent);

        private protected Type ExpressionType
        {
            get => expressionType ?? typeof(void);
        }

        public ExpressionBuilder<E> OfType(Type expressionType)
        {
            this.expressionType = expressionType;
            return this;
        }

        public ExpressionBuilder<E> OfType<T>() => OfType(typeof(T));

        public E End()
        {
            var expr = Build();
            if (treatAsStatement)
                parent.AddStatement(expr);
            return expr;
        }

        private protected abstract E Build();

        E IExpressionBuilder<E>.Build() => Build();
    }
}
