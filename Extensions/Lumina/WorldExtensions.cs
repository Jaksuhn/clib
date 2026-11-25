using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static class WorldExtensions {
    extension(World row) {
        public bool IsOnCurrentDataCenter
            => row.IsPublic && IPlayerState.Get().CurrentWorld.ValueNullable is { } current && row.DataCenter.RowId == current.DataCenter.RowId;

        public static World[] CurrentDataCenterWorlds => World.Where(w => w.IsOnCurrentDataCenter);

        public static World? FindOnCurrentDataCenter(string name)
            => World.FirstOrNull(w => w.IsOnCurrentDataCenter && w.Name.ExtractText().EqualsIgnoreCase(name));
    }
}
