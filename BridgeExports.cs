#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ExileCore;

namespace WhatsATooltip;

/// <summary>
/// Marks a method as a PluginBridge export: <c>[BridgeExport("MyPlugin.GetState")]</c>. <see cref="BridgeExports.Register"/>
/// saves it under that name as a Func&lt;...&gt; / Action&lt;...&gt; built from its signature, the delegate type consumers
/// bind with GameController.PluginBridge.GetMethod&lt;T&gt;(name).
/// Matched by type name ("BridgeExportAttribute") and its constructor's string, the way the bridge matches [Menu]: every
/// plugin carries its own copy of this file (never a reference: plugins don't compile-link each other), and a reader
/// from outside (the AI bridge) finds all of them the same way.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
internal sealed class BridgeExportAttribute : Attribute
{
    public BridgeExportAttribute(string name) => Name = name;
    public string Name { get; }
}

/// <summary>
/// Registers every [BridgeExport] method of a plugin on the HUD's PluginBridge. Copied per plugin, like HudLog.cs.
/// Scans the plugin's own class (instance and static methods, any visibility) and static methods of every other type in
/// its assembly. Call once from Initialise / OnLoad: <c>BridgeExports.Register(this, GameController.PluginBridge);</c>
/// A method that can't be exported (generic, ref/out/pointer parameters, more than 16 parameters, a name used twice) is
/// named on screen with the reason and skipped; the others are still registered.
/// </summary>
internal static class BridgeExports
{
    private const string AttributeName = "BridgeExportAttribute";
    private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly Type[] Funcs =
    {
        typeof(Func<>), typeof(Func<,>), typeof(Func<,,>), typeof(Func<,,,>), typeof(Func<,,,,>), typeof(Func<,,,,,>),
        typeof(Func<,,,,,,>), typeof(Func<,,,,,,,>), typeof(Func<,,,,,,,,>), typeof(Func<,,,,,,,,,>), typeof(Func<,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,,,,>),
        typeof(Func<,,,,,,,,,,,,,,,>), typeof(Func<,,,,,,,,,,,,,,,,>),
    };

    private static readonly Type[] Actions =
    {
        typeof(Action), typeof(Action<>), typeof(Action<,>), typeof(Action<,,>), typeof(Action<,,,>), typeof(Action<,,,,>),
        typeof(Action<,,,,,>), typeof(Action<,,,,,,>), typeof(Action<,,,,,,,>), typeof(Action<,,,,,,,,>), typeof(Action<,,,,,,,,,>),
        typeof(Action<,,,,,,,,,,>), typeof(Action<,,,,,,,,,,,>), typeof(Action<,,,,,,,,,,,,>), typeof(Action<,,,,,,,,,,,,,>),
        typeof(Action<,,,,,,,,,,,,,,>), typeof(Action<,,,,,,,,,,,,,,,>),
    };

    /// <summary>Saves the plugin's [BridgeExport] methods on <paramref name="bridge"/> (GameController.PluginBridge).
    /// Returns the names registered.</summary>
    public static IReadOnlyList<string> Register(object plugin, PluginBridge? bridge)
    {
        var prefix = $"[{plugin.GetType().Assembly.GetName().Name}] [BridgeExport] ";
        if (bridge == null)
        {
            DebugWindow.LogError($"{prefix}{plugin.GetType().Name}: GameController.PluginBridge is null, nothing registered (call Register from Initialise or later)");
            return Array.Empty<string>();
        }

        var registered = new List<string>();
        var owner = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (method, name) in Find(plugin.GetType(), prefix))
        {
            var where = $"{method.DeclaringType?.Name}.{method.Name}";
            if (owner.TryGetValue(name, out var first))
            {
                DebugWindow.LogError($"{prefix}{where}: name \"{name}\" is already exported by {first}: skipped");
                continue;
            }
            var type = DelegateTypeOf(method, out var why);
            if (type == null)
            {
                DebugWindow.LogError($"{prefix}{where} as \"{name}\": {why}: skipped");
                continue;
            }
            Delegate d;
            try
            {
                d = method.IsStatic ? Delegate.CreateDelegate(type, method) : Delegate.CreateDelegate(type, plugin, method);
            }
            catch (Exception ex)
            {
                DebugWindow.LogError($"{prefix}{where} as \"{name}\": Delegate.CreateDelegate({TypeName(type)}) failed: {ex.GetType().Name}: {ex.Message}: skipped");
                continue;
            }
            bridge.SaveMethod(name, d);
            owner[name] = where;
            registered.Add(name);
        }
        return registered;
    }

    /// <summary>[BridgeExport] methods: the plugin class (instance and static, base classes in this assembly too), then
    /// static methods of the assembly's other types. Instance methods elsewhere have no object to bind to: named, skipped.</summary>
    private static IEnumerable<(MethodInfo method, string name)> Find(Type pluginType, string prefix)
    {
        var asm = pluginType.Assembly;
        var pluginChain = new HashSet<Type>();
        for (var t = pluginType; t != null && t.Assembly == asm; t = t.BaseType) pluginChain.Add(t);

        Type?[] types;
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex)
        {
            var first = ex.LoaderExceptions.FirstOrDefault(e => e != null);
            DebugWindow.LogError($"{prefix}{asm.GetName().Name}.GetTypes() failed to load some types ({first?.Message}): exports on those types are missing");
            types = ex.Types;
        }

        foreach (var t in pluginChain.Concat(types.Where(t => t != null && !pluginChain.Contains(t!)).Select(t => t!)))
        foreach (var m in t.GetMethods(All))
        {
            var a = m.CustomAttributes.FirstOrDefault(x => x.AttributeType.Name == AttributeName);
            if (a == null) continue;
            var name = a.ConstructorArguments.Count == 1 ? a.ConstructorArguments[0].Value as string : null;
            if (string.IsNullOrWhiteSpace(name))
            {
                DebugWindow.LogError($"{prefix}{t.Name}.{m.Name}: [BridgeExport] has no name: skipped");
                continue;
            }
            if (!m.IsStatic && !pluginChain.Contains(t))
            {
                DebugWindow.LogError($"{prefix}{t.Name}.{m.Name} as \"{name}\": instance method outside the plugin class {pluginType.Name}, so there is no object to bind it to (make it static or move it): skipped");
                continue;
            }
            yield return (m, name!);
        }
    }

    /// <summary>Func&lt;params..., return&gt; or Action&lt;params...&gt; for the method; null with the reason when none fits.</summary>
    private static Type? DelegateTypeOf(MethodInfo m, out string why)
    {
        why = "";
        if (m.ContainsGenericParameters) { why = "generic method: no single delegate type to save"; return null; }
        var ps = m.GetParameters();
        foreach (var p in ps)
        {
            var t = p.ParameterType;
            if (t.IsByRef) { why = $"parameter '{p.Name}' is ref/out/in ({t.Name}): Func/Action can't carry it"; return null; }
            if (t.IsPointer) { why = $"parameter '{p.Name}' is a pointer ({t.Name}): Func/Action can't carry it"; return null; }
            if (t.IsByRefLike) { why = $"parameter '{p.Name}' is a ref struct ({t.Name}): Func/Action can't carry it"; return null; }
        }
        var ret = m.ReturnType;
        if (ret.IsByRef || ret.IsPointer || ret.IsByRefLike) { why = $"return type {ret.Name} can't be a Func result"; return null; }
        if (ps.Length > 16) { why = $"{ps.Length} parameters: Func/Action take at most 16"; return null; }

        var args = ps.Select(p => p.ParameterType);
        if (ret == typeof(void))
            return ps.Length == 0 ? typeof(Action) : Actions[ps.Length].MakeGenericType(args.ToArray());
        return Funcs[ps.Length].MakeGenericType(args.Append(ret).ToArray());
    }

    private static string TypeName(Type t) => !t.IsGenericType ? t.Name
        : $"{t.Name[..t.Name.IndexOf('`')]}<{string.Join(", ", t.GetGenericArguments().Select(TypeName))}>";
}
