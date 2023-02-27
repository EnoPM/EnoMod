using System;

namespace EnoMod;

public class EnoModException : Exception
{
    public EnoModException(string? message = null)
    {
        System.Console.WriteLine(message);
    }
}
