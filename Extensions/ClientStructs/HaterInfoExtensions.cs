using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace clib.Extensions;

public static unsafe class HaterInfoExtensions {
    extension(ref HaterInfo hater) {
        public BattleChara* GetBattleChara() => CharacterManager.Instance()->LookupBattleCharaByEntityId(hater.EntityId);
        public GameObject* GetGameObject() => GameObjectManager.Instance()->Objects.GetObjectByEntityId(hater.EntityId);
    }
}
