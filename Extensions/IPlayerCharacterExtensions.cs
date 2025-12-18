using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

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
    }
}
