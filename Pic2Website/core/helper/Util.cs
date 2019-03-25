using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Pic2Website.core
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

        public static string Repeat(char repeat, int length)
        {
            return new string(repeat, length);
        }

        public static bool SameColors(int[] first, int[] second)
        {
            if (first == null || second == null)
            {
                return false;
            }

            return first[0] == second[0] && first[1] == second[1] && first[2] == second[2];
        }
    }
}
