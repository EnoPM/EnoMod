using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnoMod.Customs;
using EnoMod.Utils;

namespace EnoMod.Kernel;

[AttributeUsage(AttributeTargets.Method)]
public class EnoHookAttribute : Attribute
{
    public CustomHooks HookType { get; }
    public int Priority { get; }

    public EnoHookAttribute(CustomHooks hookType, int priority = 0)
    {
        HookType = hookType;
        Priority = priority;
    }
}

public static class Hooks
{
    public enum Result
    {
        Continue,
        ReturnTrue,
        ReturnFalse,
    }

    public static readonly List<HookData> AllHooks = new();

    public static Result Trigger(CustomHooks hookId, params object[] arguments)
    {
        foreach (var hook in AllHooks.Where(hook => hook.Hook == hookId))
        {
            if (hookId == CustomHooks.LoadCustomOptions)
            {
                System.Console.WriteLine($"Hook ${hook.Method.Name}");
            }
            var result = hook.Invoke(arguments);
            if (result != Result.Continue)
            {
                return result;
            }
        }

        return Result.Continue;
    }

    public static void Load()
    {
        var methods = Attributes.GetMethodsByAttribute<EnoHookAttribute>();
        foreach (var method in methods)
        {
            AllHooks.Add(new HookData(method.Attribute.HookType, method.Instance, method.MethodInfo));
        }
        System.Console.WriteLine($"AllHooks: {AllHooks.Count}");
    }
}

public class HookData
{
    public CustomHooks Hook { get; }
    public object? Invoker { get; }
    public MethodInfo Method { get; }

    public bool IsStatic
    {
        get
        {
            return Invoker == null;
        }
    }

    public HookData(CustomHooks hook, object? invoker, MethodInfo method)
    {
        Hook = hook;
        Invoker = invoker;
        Method = method;
    }

    public Hooks.Result Invoke(object[] arguments)
    {
        try
        {
            var result = Method.Invoke(IsStatic ? null : Invoker, arguments);
            if (result == null)
            {
                return Hooks.Result.Continue;
            }

            return (Hooks.Result) result;
        }
        catch (Exception e)
        {
            System.Console.WriteLine($"{e.Message}\n\n\n***** {Method.DeclaringType?.FullName}::{Method.Name}\n\n===== arguments: {arguments.Length} - parameters = {Method.GetParameters().Length}");
            return Hooks.Result.Continue;
        }
    }
}
