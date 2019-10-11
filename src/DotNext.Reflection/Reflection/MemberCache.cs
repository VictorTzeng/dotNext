using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DotNext.Reflection
{
    internal class Cache<K, V> : ConcurrentDictionary<K, V>
        where V : class
    {
        private readonly Func<K, V> factory;

        private protected Cache(IEqualityComparer<K> comparer, Func<K, V> factory)
            : base(comparer)
            => this.factory = factory;

        private protected Cache(Func<K, V> factory)
            : this(EqualityComparer<K>.Default, factory)
        {
        }

        internal V GetOrAdd(K cacheKey) => GetOrAdd(cacheKey, factory);
    }

    [StructLayout(LayoutKind.Auto)]
    internal readonly struct MemberKey : IEquatable<MemberKey>
    {
        internal readonly bool NonPublic;
        internal readonly string Name;

        internal MemberKey(string name, bool nonPublic)
        {
            NonPublic = nonPublic;
            Name = name;
        }

        public bool Equals(MemberKey other) => NonPublic == other.NonPublic && Name == other.Name;

        public override bool Equals(object other) => other is MemberKey key && Equals(key);

        public override int GetHashCode()
        {
            var hashCode = -910176598;
            hashCode = hashCode * -1521134295 + NonPublic.GetHashCode();
            hashCode = hashCode * -1521134295 + Name?.GetHashCode() ?? 0;
            return hashCode;
        }
    }

    internal abstract class MemberCache<M, E> : Cache<MemberKey, E>
        where M : MemberInfo
        where E : class, IMember<M>
    {
        private static readonly UserDataSlot<MemberCache<M, E>> Slot = UserDataSlot<MemberCache<M, E>>.Allocate();

        private protected MemberCache(Func<MemberKey, E> factory)
            : base(factory)
        {
        }

        internal E GetOrAdd(string memberName, bool nonPublic) => GetOrAdd(new MemberKey(memberName, nonPublic));

        internal static MemberCache<M, E> Of<C>(MemberInfo member)
            where C : MemberCache<M, E>, new()
            => member.GetUserData().GetOrSet<MemberCache<M, E>, C>(Slot);
    }
}
