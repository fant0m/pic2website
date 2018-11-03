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
    }
}
