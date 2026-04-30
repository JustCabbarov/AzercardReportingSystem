using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RMS.Application.Extensions
{
    
    internal static class EnumerableExtensions
    {
        internal static IEnumerable<(T, T2, T3)> Zip<T, T2, T3>(
            this IEnumerable<T> first,
            IEnumerable<T2> second,
            IEnumerable<T3> third)
            => first.Zip(second).Zip(third, (ab, c) => (ab.First, ab.Second, c));
    }
}
