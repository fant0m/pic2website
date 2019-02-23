using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RazorPagesMovie.core
{
    public static class Util
    {
        public static T MostCommon<T>(this IEnumerable<T> list)
        {
            var most = list.GroupBy(j => j)
                .OrderByDescending(grp => grp.Count())
                .Select(grp => grp.Key)
                .First();

            return most;
        }

        public static bool AreSame(double value1, double value2, int diff = 2)
        {
            return Math.Abs(value2 - value1) <= diff;
        }
    }
}
