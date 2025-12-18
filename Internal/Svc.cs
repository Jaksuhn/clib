using Dalamud.Game;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs;

namespace clib.Internal;

internal class Svc {
    [PluginService] public static IDalamudPluginInterface Interface { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
    [PluginService] public static IKeyState KeyState { get; private set; } = null!;
    [PluginService] public static IObjectTable Objects { get; private set; } = null!;
    [PluginService] public static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;

    public static NavmeshIPC Navmesh { get; private set; } = null!;
    public static void Init(IDalamudPluginInterface pi) {
        pi.Create<Svc>();
        Navmesh = new NavmeshIPC();
    }
}

internal static class LogExtensions {
    public static void Print(this IPluginLog log, string message) => log.Debug($"[{nameof(clib)}] {message}");
    public static void PrintWarning(this IPluginLog log, string message) => log.Warning($"[{nameof(clib)}] {message}");
    public static void PrintError(this IPluginLog log, string message) => log.Error($"[{nameof(clib)}] {message}");
}
