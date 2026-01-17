using Lumina.Excel;

namespace clib.Extensions;

public static class IExcelRowExtensions {
    extension<T>(IExcelRow<T> sheet) where T : struct, IExcelRow<T> {
        public static RowRef<T> GetRowRef(uint id, Lumina.Data.Language? language = null)
            => new(Svc.Data.Excel, id, language);

        public static T GetRow(uint id, Dalamud.Game.ClientLanguage? language = null)
            => Svc.Data.GetExcelSheet<T>(language: language).GetRow(id);
    }
}
