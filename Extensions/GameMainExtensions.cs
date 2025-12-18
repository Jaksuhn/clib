using FFXIVClientStructs.FFXIV.Client.Game;

namespace clib.Extensions;

public static unsafe class GameMainExtensions {
    extension(GameMain instance) {
        public static bool IsTerritoryLoaded => GameMain.Instance()->TerritoryLoadState == 2;
    }
}
