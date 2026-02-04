using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Network;

namespace clib.TaskSystem;

internal interface IHookManager : IDisposable { }

internal abstract class HookManagerBase : IHookManager {
    private bool _disposed;

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;
        DisposeManaged();
        GC.SuppressFinalize(this);
    }

    protected abstract void DisposeManaged();
}

internal sealed unsafe class CastHookManager : HookManagerBase {
    private static readonly Lazy<CastHookManager> _instance = new(() => new CastHookManager());
    internal static CastHookManager Instance => _instance.Value;

    private volatile ActionType _watchedType;
    private volatile uint _watchedId;

    internal volatile bool LastCastCompleted;
    internal volatile bool LastCastCancelled;

    private readonly Hook<PacketDispatcher.Delegates.HandleActorControlPacket> _actorControlHook;

    private delegate void CharacterCompleteCastDelegate(GameObject* source, ActionType actionType, uint actionId, int a4, GameObjectId objectId, float* a6, float value, ushort a8, int a9, uint entityId);
    private readonly Hook<CharacterCompleteCastDelegate> _completeCastHook;

    private CastHookManager() {
        var completeCastAddr = Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 41 0F B6 56 ?? 44 0F 28 8C 24 ?? ?? ?? ??");

        _actorControlHook = Svc.Hook.HookFromAddress<PacketDispatcher.Delegates.HandleActorControlPacket>((nint)PacketDispatcher.MemberFunctionPointers.HandleActorControlPacket, ProcessPacketActorControlDetour);
        _completeCastHook = Svc.Hook.HookFromAddress<CharacterCompleteCastDelegate>(completeCastAddr, CharacterCompleteCastDetour);
    }

    protected override void DisposeManaged() {
        _actorControlHook.Dispose();
        _completeCastHook.Dispose();
    }

    internal void Enable() {
        _actorControlHook.Enable();
        _completeCastHook.Enable();
    }

    internal void Disable() {
        _actorControlHook.Disable();
        _completeCastHook.Disable();
        ClearWatching();
    }

    internal void StartWatching(ActionType type, uint id) {
        _watchedType = type;
        _watchedId = id;
        LastCastCompleted = false;
        LastCastCancelled = false;
    }

    internal void ClearWatching() {
        _watchedId = 0;
        LastCastCompleted = false;
        LastCastCancelled = false;
    }

    private void ProcessPacketActorControlDetour(uint entityId, uint category, uint a1, uint a2, uint a3, uint a4, uint a5, uint a6, uint a7, uint a8, GameObjectId targetId, bool isRecorded) {
        if (Svc.Objects.LocalPlayer is not { } player) {
            _actorControlHook.Original(entityId, category, a1, a2, a3, a4, a5, a6, a7, a8, targetId, isRecorded);
            return;
        }

        // 15 = CancelCast
        if (_watchedId != 0 && entityId == player.EntityId && category == 15) {
            LastCastCancelled = true;
        }

        _actorControlHook.Original(entityId, category, a1, a2, a3, a4, a5, a6, a7, a8, targetId, isRecorded);
    }

    private void CharacterCompleteCastDetour(GameObject* source, ActionType actionType, uint actionId, int a4, GameObjectId objectId, float* a6, float value, ushort a8, int a9, uint entityId) {
        if (source->GetGameObjectId() != Svc.Objects.LocalPlayer?.GameObjectId) {
            _completeCastHook.Original(source, actionType, actionId, a4, objectId, a6, value, a8, a9, entityId);
            return;
        }

        if (_watchedId == 0) {
            _completeCastHook.Original(source, actionType, actionId, a4, objectId, a6, value, a8, a9, entityId);
            return;
        }

        // casting mount roulette calls ActionType.Mount with the mount id instead of the general action
        var matches = actionId == _watchedId || _watchedType == ActionType.GeneralAction && _watchedId == 24 && actionType == ActionType.Mount;
        if (matches) {
            LastCastCompleted = true;
        }

        _completeCastHook.Original(source, actionType, actionId, a4, objectId, a6, value, a8, a9, entityId);
    }
}

