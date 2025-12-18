using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;

namespace clib.Extensions;

public static unsafe class ActionManagerExtensions {
    extension(ActionManager) {
        public static bool IsCasting(ActionType actionType, uint actionId) {
            if (Svc.Objects.LocalPlayer is not { } player) return false;
            var info = player.Address.As<Character>()->GetCastInfo();
            return info is not null && info->IsCasting && info->ActionType == (byte)actionType && info->ActionId == actionId;
        }

        public static bool UseAction(ActionType actionType, uint actionId) {
            var am = ActionManager.Instance();
            return am is not null && am->UseAction(actionType, actionId);
        }

        public static bool IsActionInUse(ActionType type, uint itemId) {
            var am = ActionManager.Instance();
            return am is not null && am->GetActionStatus(type, itemId) != 0;
        }
    }
}
