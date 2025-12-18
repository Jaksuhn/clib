using Dalamud.Game.ClientState.Fates;

namespace clib.Extensions;

public static class IFateExtensions {
    extension(IFate fate) {
        public ItemWrapper? EventItem => fate.GameData.Value.EventItem.RowId is not 0 ? fate.GameData.Value.EventItem : null;
        public int EventItemInventoryCount => fate.EventItem?.GetCount() ?? 0;
    }
}
