using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using System.Runtime.CompilerServices;

namespace clib.Extensions;

public static unsafe class GameObjectExtensions {
    extension(ref GameObject obj) {
        public void ResetEnmity() {
            if (!ICondition.Get()[ConditionFlag.InCombat])
                return;

            var bc = obj.BattleChara();
            if (bc != null && bc->NameId is 541)
                GameMain.ExecuteCommand(CommandFlag.ResetStrikingDummy, (int)obj.EntityId);
        }
    }

    public static BattleChara* BattleChara(ref this GameObject obj) => (BattleChara*)Unsafe.AsPointer(ref obj);
    public static Character* Character(ref this GameObject obj) => (Character*)Unsafe.AsPointer(ref obj);
}
