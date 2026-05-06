using Dalamud.Plugin;

namespace clib;

public static class CLibMain {
    public static string Name { get; private set; } = null!;
    public static void Init(IDalamudPluginInterface pi, object instance) {
        if (instance is not (IDalamudPlugin or IAsyncDalamudPlugin))
            throw new InvalidOperationException($"Invalid plugin instance. Must be of type {nameof(IDalamudPlugin)} or {nameof(IAsyncDalamudPlugin)}");
        Svc.Init(pi);
        Name = instance.GetType().Name;
    }
}
