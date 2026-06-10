using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;

namespace clib.Extensions;

public static class IObjectTableExtensions {
    public static bool TryGetByGameObjectId(this IObjectTable objs, ulong gameObjectId, out IGameObject obj) {
        if (objs.FirstOrDefault(o => o.GameObjectId == gameObjectId) is { } match) {
            obj = match;
            return true;
        }
        obj = default!;
        return false;
    }

    public static unsafe bool TryGetPlayerByContentId(this IObjectTable objs, ulong contentId, out IGameObject obj) {
        if (objs.OfType<IPlayerCharacter>().FirstOrDefault(o => o.Character->ContentId == contentId) is { } match) {
            obj = match;
            return true;
        }
        obj = default!;
        return false;
    }
}
