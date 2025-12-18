using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static class TerritoryTypeExtensions {
    extension(TerritoryType row) {
        public bool AllowsFlight => row.AetherCurrentCompFlgSet.RowId != 0;
        public bool IsWorkshop => row.BGM.RowId is 328;
    }
}
