using FFXIVClientStructs.FFXIV.Client.Game.Control;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace clib.Extensions;

public static unsafe class TargetSystemExtensions {
    extension(TargetSystem) {
        public static bool InteractWith(ulong instanceId) {
            var obj = GameObjectManager.Instance()->Objects.GetObjectByGameObjectId(instanceId);
            if (obj == null)
                return false;
            TargetSystem.Instance()->InteractWithObject(obj, false);
            return true;
        }
    }
}
