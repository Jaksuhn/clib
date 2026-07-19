using AllaganLib.GameSheets.Service;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace clib.Services;

public class Svc {
    [PluginService] public static IAddonEventManager AddonEventManager { get; private set; } = null!;
    [PluginService] public static IAddonLifecycle AddonLifecycle { get; private set; } = null!;
    [PluginService] public static IAetheryteList AetheryteList { get; private set; } = null!;
    [PluginService] public static IAgentLifecycle AgentLifecycle { get; private set; } = null!;
    [PluginService] public static IDalamudPluginInterface Interface { get; private set; } = null!;
    [PluginService] public static IBuddyList Buddies { get; private set; } = null!;
    [PluginService] public static IChatGui Chat { get; private set; } = null!;
    [PluginService] public static IClientState ClientState { get; private set; } = null!;
    [PluginService] public static ICommandManager Commands { get; private set; } = null!;
    [PluginService] public static ICondition Condition { get; private set; } = null!;
    [PluginService] public static IContextMenu ContextMenu { get; private set; } = null!;
    [PluginService] public static IDataManager Data { get; private set; } = null!;
    [PluginService] public static IDtrBar DtrBar { get; private set; } = null!;
    [PluginService] public static IDutyState DutyState { get; private set; } = null!;
    [PluginService] public static IFateTable Fates { get; private set; } = null!;
    [PluginService] public static IFlyTextGui FlyText { get; private set; } = null!;
    [PluginService] public static IFramework Framework { get; private set; } = null!;
    [PluginService] public static IGameConfig GameConfig { get; private set; } = null!;
    [PluginService] public static IGameGui GameGui { get; private set; } = null!;
    [PluginService] public static IGameInteropProvider Hook { get; private set; } = null!;
    [PluginService] public static IGameInventory GameInventory { get; private set; } = null!;
    [PluginService] public static IGameLifecycle GameLifecycle { get; private set; } = null!;
    [PluginService] public static IGamepadState GamepadState { get; private set; } = null!;
    [PluginService] public static IJobGauges Gauges { get; private set; } = null!;
    [PluginService] public static IKeyState KeyState { get; private set; } = null!;
    [PluginService] public static IMarketBoard MarketBoard { get; private set; } = null!;
    [PluginService] public static INamePlateGui NamePlates { get; private set; } = null!;
    [PluginService] public static INotificationManager NotificationManager { get; private set; } = null!;
    [PluginService] public static IObjectTable Objects { get; private set; } = null!;
    [PluginService] public static IPartyFinderGui PfGui { get; private set; } = null!;
    [PluginService] public static IPartyList Party { get; private set; } = null!;
    [PluginService] public static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] public static IPluginLog Log { get; private set; } = null!;
    [PluginService] public static IReliableFileStorage ReliableFileStorage { get; private set; } = null!;
    [PluginService] public static ISeStringEvaluator SeStringEvaluator { get; private set; } = null!;
    [PluginService] public static ISigScanner SigScanner { get; private set; } = null!;
    [PluginService] public static ITargetManager Targets { get; private set; } = null!;
    [PluginService] public static ITextureProvider Texture { get; private set; } = null!;
    [PluginService] public static ITextureSubstitutionProvider TextureSubstitution { get; private set; } = null!;
    [PluginService] public static ITextureReadbackProvider TextureReadback { get; private set; } = null!;
    [PluginService] public static ITitleScreenMenu TitleScreenMenu { get; private set; } = null!;
    [PluginService] public static IToastGui Toasts { get; private set; } = null!;
    [PluginService] public static IUnlockState UnlockState { get; private set; } = null!;

    public static ItemService Items { get; private set; } = null!;
    public static Automation Automation { get; private set; } = null!;
    public static SheetManager SheetManager { get; private set; } = null!;

    internal static NavmeshIPC Navmesh { get; private set; } = null!;
    internal static Hooks InternalHooks { get; private set; } = null!;

    private static readonly ConcurrentDictionary<Type, object> Singletons = new();

    public static void Register<T>() where T : class, new()
        => Register(() => new T());

    public static void Register<T>(Func<T> singleton) where T : class {
        ArgumentNullException.ThrowIfNull(singleton);
        var key = typeof(T);
        var instance = singleton();
        if (!Singletons.TryAdd(key, instance))
            throw new InvalidOperationException($"[{nameof(Svc)}] {key.FullName} is already registered.");
    }

    public static T Get<T>() where T : class {
        if (!Singletons.TryGetValue(typeof(T), out var instance))
            throw new InvalidOperationException($"[{nameof(Svc)}] {typeof(T).FullName} has not been registered.");
        return (T)instance;
    }

    internal static void Init(IDalamudPluginInterface pi, CLibModule modules) {
        pi.Create<Svc>();
        Navmesh = new NavmeshIPC();
        InternalHooks = new Hooks();

        if (modules.HasFlag(CLibModule.SheetManager))
            SheetManager = new(pi, Data.GameData, new());
        if (modules.HasFlag(CLibModule.Items))
            Items = new();
        if (modules.HasFlag(CLibModule.Automation))
            Automation = new();
    }

    internal static async ValueTask DisposeAsync() {
        await DisposeObjectAsync(Items).ConfigureAwait(false);
        await DisposeObjectAsync(Automation).ConfigureAwait(false);
        await DisposeObjectAsync(SheetManager).ConfigureAwait(false);
        await DisposeObjectAsync(InternalHooks).ConfigureAwait(false);

        foreach (var s in Singletons.Values) {
            try {
                await DisposeObjectAsync(s).ConfigureAwait(false);
            }
            catch {
                Log.Error($"[{nameof(Svc)}] Failed disposal of {s.GetType().FullName}");
            }
        }
    }

    internal static void Dispose()
        => DisposeAsync().ConfigureAwait(false).GetAwaiter().GetResult();

    private static ValueTask DisposeObjectAsync(object? obj) {
        if (obj is null) return ValueTask.CompletedTask;
        if (obj is IAsyncDisposable asyncDisposable)
            return asyncDisposable.DisposeAsync();
        if (obj is IDisposable disposable) {
            disposable.Dispose();
            return ValueTask.CompletedTask;
        }
        return ValueTask.CompletedTask;
    }
}

internal static class LogExtensions {
    public static void Print(this IPluginLog log, string message) => log.Debug($"[{nameof(clib)}] {message}");
    public static void PrintWarning(this IPluginLog log, string message) => log.Warning($"[{nameof(clib)}] {message}");
    public static void PrintError(this IPluginLog log, string message) => log.Error($"[{nameof(clib)}] {message}");
}
