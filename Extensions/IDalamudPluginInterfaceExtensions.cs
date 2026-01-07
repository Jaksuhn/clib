using Dalamud.Plugin;
using System.Reflection;

namespace clib.Extensions;

public static class IDalamudPluginInterfaceExtensions {

    public static object? GetService(this IDalamudPluginInterface @interface, string serviceName)
        => @interface.GetType().Assembly.GetType("Dalamud.Service`1", true)?
        .MakeGenericType(@interface.GetType().Assembly.GetType(serviceName, true)!)
        .GetMethod("Get")?.Invoke(null, BindingFlags.Default, null, [], null);

    public static void ToggleDtr(this IDalamudPluginInterface @interface) {
        var config = @interface.GetService("Dalamud.Configuration.Internal.DalamudConfiguration");
        if (config?.GetFieldOrProperty<List<string>>("DtrIgnore") is { } ignoreList) {
            if (!ignoreList.Contains(@interface.InternalName))
                ignoreList.Remove(@interface.InternalName);
            else
                ignoreList.Add(@interface.InternalName);
            config.CallMethod("QueueSave", []);
        }
    }
}
