using System;

namespace MissingPieces
{
    public class GenericArgumentException: ArgumentException
    {
        protected GenericArgumentException(Type genericParam, string message)
            : base(message)
        {
        }

        public Type Argument { get; }

        public static GenericArgumentException Create<T>(string message)
            => new GenericArgumentException(typeof(T), message);
    }
}