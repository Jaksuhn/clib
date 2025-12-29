using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using System.Runtime.InteropServices;

namespace clib.Extensions;

public static unsafe class IPlayerCharacterExtensions {
    extension(IPlayerCharacter? pc) {
        public Character* Character => (pc?.Address ?? nint.Zero).As<Character>();
        public bool Available => pc != null;
        public bool Interactable => pc?.IsTargetable ?? false;
        public bool IsMoving => get_Available(pc) && (AgentMap.Instance()->IsPlayerMoving || get_IsJumping(pc));
        public bool IsJumping => get_Available(pc) && (Svc.Condition[ConditionFlag.Jumping] || Svc.Condition[ConditionFlag.Jumping61] || get_Character(pc)->IsJumping());
        public bool IsBusy
            => Svc.Condition.IsUnavailable() ||
            !get_Interactable(pc) ||
            (pc?.IsCasting ?? false) ||
            get_IsMoving(pc) ||
            ActionManager.Instance()->AnimationLock > 0 ||
            Svc.Condition[ConditionFlag.InCombat] ||
            !GameMain.IsTerritoryLoaded;

        public bool Mounted => Svc.Condition[ConditionFlag.Mounted];
        public bool InFlight => Svc.Condition[ConditionFlag.InFlight];
        public float PackedRotation => (ushort)(((Svc.Objects.LocalPlayer?.Rotation + Math.PI) / (2 * Math.PI) * 65536) ?? 0);
    }

    public static void SetSpeed(this IPlayerCharacter _, float speedBase) {
        Svc.SigScanner.TryScanText("F3 0F 11 05 ?? ?? ?? ?? 40 38 2D", out var address);
        address = address + 4 + Marshal.ReadInt32(address + 4) + 4;
        Dalamud.SafeMemory.Write(address + 20, speedBase);
        SetMoveControlData(speedBase);
    }

    private static unsafe void SetMoveControlData(float speed)
        => Dalamud.SafeMemory.Write(((delegate* unmanaged[Stdcall]<byte, nint>)Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 48 85 C0 74 AE 83 FD 05"))(1) + 8, speed);
}
