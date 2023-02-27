using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EnoMod.Roles;

namespace EnoMod.Modules;

[AttributeUsage(AttributeTargets.Method)]
public class RoleHookAttribute : Attribute
{
    public Hooks.AmongUs HookType { get; }

    public RoleHookAttribute(Hooks.AmongUs hookType)
    {
        HookType = hookType;
    }
}

public class Hooks
{
    public enum Result
    {
        Continue,
        ReturnTrue,
        ReturnFalse,
    }

    public enum AmongUs
    {
        AdminTableOpened,
        CamerasUpdated,
        PlanetCameraUpdated,
        PlanetCameraNextUpdated,
        VitalsUpdated,
        MeetingEnded,
    }

    public static readonly List<HookData> AllHooks = new();

    public static Result Trigger(AmongUs hookId, params object[] arguments)
    {
        foreach (var hook in AllHooks.Where(hook => hook.Hook == hookId))
        {
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
        var fieldsInfos = typeof(Reference).GetFields(BindingFlags.Static | BindingFlags.Public);
        System.Console.WriteLine(fieldsInfos.Length);
        var instances = new List<CustomRole>();
        foreach (var field in fieldsInfos)
        {
            System.Console.WriteLine($"Property: {field.Name} {field.FieldType.Name} {field.FieldType.IsSubclassOf(typeof(CustomRole))}");
            if (field.FieldType.IsSubclassOf(typeof(CustomRole)) && field.Name == field.FieldType.Name)
            {
                var customRole = (CustomRole?) field.GetValue(null);
                if (customRole != null)
                {
                    instances.Add(customRole);
                }
            }
        }

        System.Console.WriteLine($"Instances: {instances.Count}");

        foreach (var instance in instances)
        {
            var methods = instance.GetType().GetMethods().Where(method =>
                method.GetCustomAttributes(typeof(RoleHookAttribute), false).FirstOrDefault() != null).ToList();
            foreach (var method in methods)
            {
                var attributes = method.GetCustomAttributes(typeof(RoleHookAttribute))
                    .Select(attribute => (RoleHookAttribute) attribute);
                foreach (var attribute in attributes)
                {
                    AllHooks.Add(new HookData(attribute.HookType, instance, method));
                    System.Console.WriteLine($"Hook: {instance.GetType().Name}.{method.Name}");
                }
            }
        }

        System.Console.WriteLine($"AllHooks: {AllHooks.Count}");
    }
}

public class HookData
{
    public Hooks.AmongUs Hook { get; }
    public CustomRole? Invoker { get; }
    public MethodInfo Method { get; }

    public bool IsStatic
    {
        get
        {
            return Invoker == null;
        }
    }

    public HookData(Hooks.AmongUs hook, CustomRole invoker, MethodInfo method)
    {
        Hook = hook;
        Invoker = invoker;
        Method = method;
    }

    public Hooks.Result Invoke(object[] arguments)
    {
        var result = Method.Invoke(IsStatic ? null : Invoker, arguments);
        if (result == null)
        {
            return Hooks.Result.Continue;
        }

        return (Hooks.Result) result;
    }
}
