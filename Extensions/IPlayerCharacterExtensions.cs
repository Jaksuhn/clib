using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using System.Runtime.InteropServices;

namespace clib.Extensions;

public static unsafe class IPlayerCharacterExtensions {
    private unsafe delegate byte IsAirDismountableDelegate(UIState* uiState, FFXIVClientStructs.FFXIV.Common.Math.Vector3* position);

    extension(IPlayerCharacter? pc) {
        public Character* Character => (pc?.Address ?? nint.Zero).As<Character>();
        public bool Available => pc != null;
        public bool Interactable => pc?.IsTargetable ?? false;
        public bool IsMoving => get_Available(pc) && (AgentMap.Instance()->IsPlayerMoving || get_IsJumping(pc));
        public bool IsJumping => get_Available(pc) && (Svc.Condition[ConditionFlag.Jumping] || Svc.Condition[ConditionFlag.Jumping61] || get_Character(pc)->IsJumping());
        public bool IsAirDismountable {
            get {
                var pos = Control.GetLocalPlayer()->Position;
                return Marshal.GetDelegateForFunctionPointer<IsAirDismountableDelegate>(Svc.SigScanner.ScanText("E8 ?? ?? ?? ?? 84 C0 75 24 4D 85 F6"))(UIState.Instance(), &pos) == 1;
            }
        }

        //public bool IsAirDismountable => UIState.Instance()->IsAirDismountable;
        public bool IsBusy
            => Svc.Condition.IsUnavailable() ||
            !get_Interactable(pc) ||
            (pc?.IsCasting ?? false) ||
            get_IsMoving(pc) ||
            ActionManager.Instance()->AnimationLock > 0 ||
            Svc.Condition[ConditionFlag.InCombat] ||
            !GameMain.IsTerritoryLoaded;

        public RowRef<TerritoryType> Territory => Svc.Data.GetRef<TerritoryType>(Svc.ClientState.TerritoryType);

        public bool Mounted => Svc.Condition[ConditionFlag.Mounted];
        public bool InFlight => Svc.Condition[ConditionFlag.InFlight];
        /// <summary>
        /// Rotation packed into a ushort. Used in some <see cref="GameMain.ExecuteCommand"/> functions.
        /// </summary>
        public float PackedRotation => (ushort)(((Svc.Objects.LocalPlayer?.Rotation + Math.PI) / (2 * Math.PI) * 65536) ?? 0);
    }
}
