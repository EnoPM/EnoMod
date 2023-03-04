using System;
using System.Collections.Generic;
using EnoMod.Utils;

namespace EnoMod.Kernel;

[AttributeUsage(AttributeTargets.Class)]
public class EnoSingletonAttribute : Attribute
{
}

public static class Singleton<T>
{
    public static T Instance
    {
        get
        {
            return (T) Instances.Get<T>();
        }
        set
        {
            if (value == null)
                throw new EnoModException($"Cannot set singleton of {typeof(T).FullName} with null value");
            Instances.Set((T) value);
        }
    }
}

public static class Instances
{
    public static readonly Dictionary<Type, object> Singletons = new();

    public static object Get<T>()
    {
        return Singletons[typeof(T)];
    }

    public static bool Has(Type type)
    {
        return Singletons.ContainsKey(type);
    }

    public static object Set(object value)
    {
        return Singletons[value.GetType()] = value;
    }

    public static void Load()
    {
        var classResults = Attributes.GetClassesByAttribute<EnoSingletonAttribute>();
        foreach (var classResult in classResults)
        {
            Set(Attributes.GetInstance(classResult.Type));
        }
    }
}
