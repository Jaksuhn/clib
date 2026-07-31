using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using IntendedUse = FFXIVClientStructs.FFXIV.Client.Enums.TerritoryIntendedUse;

namespace clib.Extensions;

public static class IPlayerStateExtensions {
    extension(IPlayerState ps) {
        public unsafe RowRef<TerritoryType> Territory
            => ps.IsLoaded && GameMain.Instance()->TerritoryLoadState is 2 ? TerritoryType.GetRowRef(GameMain.Instance()->CurrentTerritoryTypeId) : default;
        public unsafe IntendedUse TerritoryIntendedUse
            => ps.IsLoaded && GameMain.Instance()->TerritoryLoadState is 2 ? GameMain.Instance()->CurrentTerritoryIntendedUseId : unchecked((IntendedUse)(-1));
        public bool IsInSoloDuty
            => ps.IsLoaded && get_Territory(ps).IsValid && get_Territory(ps).ValueNullable?.TerritoryIntendedUse.ValueNullable?.StructsEnum is IntendedUse.SoloDuty;

        public unsafe bool IsBuddyInStable => ps.IsLoaded && PlayerState.Instance()->IsPlayerStateFlagSet(PlayerStateFlag.IsBuddyInStable);
    }
}
