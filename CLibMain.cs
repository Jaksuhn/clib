using Dalamud.Plugin;

namespace clib;

public static class CLibMain {
    public static string Name { get; private set; } = null!;
    public static void Init(IDalamudPluginInterface pi, IDalamudPlugin instance) {
        Svc.Init(pi);
        Name = instance.GetType().Name;
    }

    public static void Dispose() {
        Svc.CastHooks.Dispose();
    }
}
