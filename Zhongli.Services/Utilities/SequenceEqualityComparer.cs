using System;
using System.Collections.Generic;
using System.Linq;

namespace Zhongli.Services.Utilities
{
    public class SequenceEqualityComparer<T> : IEqualityComparer<IReadOnlyCollection<T>>
    {
        public bool Equals(IReadOnlyCollection<T>? x, IReadOnlyCollection<T>? y)
            => x is not null && y is not null && x.SequenceEqual(y);

        public int GetHashCode(IReadOnlyCollection<T> obj)
        {
            var hashCode = new HashCode();

            foreach (var item in obj)
            {
                hashCode.Add(item);
            }

            return hashCode.ToHashCode();
        }
    }
}