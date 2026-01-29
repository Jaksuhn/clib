using Lumina.Excel;

namespace clib.Extensions;

public static class IExcelRowExtensions {
    extension<T>(IExcelRow<T> row) where T : struct, IExcelRow<T> {
        public T WithLanguage(Dalamud.Game.ClientLanguage language)
            => Svc.Data.GetExcelSheet<T>(language: language).GetRow(row.RowId);

        public T WithLanguage(Lumina.Data.Language language)
            => Svc.Data.GetExcelSheet<T>(language: (Dalamud.Game.ClientLanguage)language).GetRow(row.RowId);

        public static RowRef<T> GetRowRef(uint id, Lumina.Data.Language? language = null)
            => new(Svc.Data.Excel, id, language);

        public static T GetRow(uint id, Dalamud.Game.ClientLanguage? language = null)
            => Svc.Data.GetExcelSheet<T>(language: language).GetRow(id);

        public static bool Any(Func<T, bool> predicate)
            => Svc.Data.GetExcelSheet<T>().Any(r => predicate(r));

        public static bool All(Func<T, bool> predicate)
            => Svc.Data.GetExcelSheet<T>().All(r => predicate(r));

        public static IEnumerable<T> Where(Func<T, bool> predicate)
            => Svc.Data.GetExcelSheet<T>().Where(r => predicate(r));
    }
}
