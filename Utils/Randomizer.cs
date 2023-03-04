using System.Collections.Generic;

namespace EnoMod.Utils;

public static class Randomizer
{
    public static List<T> Randomize<T>(this List<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = EnoModPlugin.Rnd.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }

        return list;
    }
}
