using Dalamud.Game;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Extensions;

namespace clib.Extensions;

// https://github.com/Haselnussbomber/HaselCommon/blob/main/HaselCommon/Services/ExcelService.cs
public static class IDataManagerExtensions {
    public static RowRef<T> GetRef<T>(this IDataManager data, uint rowId, ClientLanguage? language = null) where T : struct, IExcelRow<T>
        => new(data.Excel, rowId, (language ?? Svc.ClientState.ClientLanguage).ToLumina());

    public static ExcelSheet<T> GetSheet<T>(this IDataManager data, ClientLanguage? language = null) where T : struct, IExcelRow<T>
        => data.GetExcelSheet<T>(language ?? Svc.ClientState.ClientLanguage)!;

    public static ExcelSheet<T> GetSheet<T>(this IDataManager data, string sheetName, ClientLanguage? language = null) where T : struct, IExcelRow<T>
        => data.GetExcelSheet<T>(language ?? Svc.ClientState.ClientLanguage, sheetName)!;

    public static T? GetRow<T>(this IDataManager data, uint rowId, ClientLanguage? language = null) where T : struct, IExcelRow<T>
        => GetSheet<T>(data, language).GetRowOrDefault(rowId);

    public static bool TryGetRow<T>(this IDataManager data, uint rowId, out T row) where T : struct, IExcelRow<T>
        => TryGetRow(data, rowId, null, out row);

    public static bool TryGetRow<T>(this IDataManager data, uint rowId, ClientLanguage? language, out T row) where T : struct, IExcelRow<T>
        => GetSheet<T>(data, language ?? Svc.ClientState.ClientLanguage).TryGetRow(rowId, out row);

    public static bool TryGetRow<T>(this IDataManager data, string sheetName, uint rowId, out T row) where T : struct, IExcelRow<T>
        => TryGetRow(data, sheetName, rowId, null, out row);

    public static bool TryGetRow<T>(this IDataManager data, string sheetName, uint rowId, ClientLanguage? language, out T row) where T : struct, IExcelRow<T>
        => GetSheet<T>(data, sheetName, language ?? Svc.ClientState.ClientLanguage).TryGetRow(rowId, out row);

    public static bool TryFindRow<T>(this IDataManager data, string sheetName, Predicate<T> predicate, out T row) where T : struct, IExcelRow<T>
        => TryFindRow(data, sheetName, predicate, null, out row);

    public static bool TryFindRow<T>(this IDataManager data, string sheetName, Predicate<T> predicate, ClientLanguage? language, out T row) where T : struct, IExcelRow<T>
        => GetSheet<T>(data, sheetName, language ?? Svc.ClientState.ClientLanguage).TryGetFirst(predicate, out row);

    public static bool TryFindRow<T>(this IDataManager data, Predicate<T> predicate, out T row) where T : struct, IExcelRow<T>
        => TryFindRow(data, predicate, null, out row);

    public static bool TryFindRow<T>(this IDataManager data, Predicate<T> predicate, ClientLanguage? language, out T row) where T : struct, IExcelRow<T>
        => GetSheet<T>(data, language ?? Svc.ClientState.ClientLanguage).TryGetFirst(predicate, out row);

    public static T? FindRow<T>(this IDataManager data, Func<T, bool> predicate) where T : struct, IExcelRow<T>
         => GetSheet<T>(data).FirstOrNull(row => predicate(row));

    public static IReadOnlyList<T> FindRows<T>(this IDataManager data, Predicate<T> predicate, ClientLanguage? language = null) where T : struct, IExcelRow<T>
        => [.. GetSheet<T>(data, language ?? Svc.ClientState.ClientLanguage).Where(row => predicate(row))];

    public static bool TryFindRows<T>(this IDataManager data, Predicate<T> predicate, out IReadOnlyList<T> rows) where T : struct, IExcelRow<T>
        => TryFindRows(data, predicate, null, out rows);

    public static bool TryFindRows<T>(this IDataManager data, Predicate<T> predicate, ClientLanguage? language, out IReadOnlyList<T> rows) where T : struct, IExcelRow<T> {
        rows = [.. GetSheet<T>(data, language).Where(row => predicate(row))];
        return rows.Count != 0;
    }

    // Subrow Sheets

    public static SubrowRef<T> GetSubRef<T>(this IDataManager data, uint rowId, ClientLanguage? language = null) where T : struct, IExcelSubrow<T>
        => new(data.Excel, rowId, (language ?? Svc.ClientState.ClientLanguage).ToLumina());

    public static SubrowExcelSheet<T> GetSubrowSheet<T>(this IDataManager data, ClientLanguage? language = null) where T : struct, IExcelSubrow<T>
        => data.GetSubrowExcelSheet<T>(language ?? Svc.ClientState.ClientLanguage)!;

    public static SubrowExcelSheet<T> GetSubrowSheet<T>(this IDataManager data, string sheetName, ClientLanguage? language = null) where T : struct, IExcelSubrow<T>
        => data.GetSubrowExcelSheet<T>(language ?? Svc.ClientState.ClientLanguage, sheetName)!;

    public static T? GetRow<T>(this IDataManager data, uint rowId, ushort subRowId, ClientLanguage? language = null) where T : struct, IExcelSubrow<T>
        => GetSubrowSheet<T>(data, language).GetSubrowOrDefault(rowId, subRowId);

    public static bool TryGetSubrows<T>(this IDataManager data, uint rowId, out SubrowCollection<T> rows) where T : struct, IExcelSubrow<T>
        => TryGetSubrows(data, rowId, null, out rows);

    public static bool TryGetSubrows<T>(this IDataManager data, uint rowId, ClientLanguage? language, out SubrowCollection<T> rows) where T : struct, IExcelSubrow<T>
        => GetSubrowSheet<T>(data, language ?? Svc.ClientState.ClientLanguage).TryGetRow(rowId, out rows);

    public static bool TryGetSubrow<T>(this IDataManager data, uint rowId, int subRowIndex, out T row) where T : struct, IExcelSubrow<T>
        => TryGetSubrow(data, rowId, subRowIndex, null, out row);

    public static bool TryGetSubrow<T>(this IDataManager data, uint rowId, int subRowIndex, ClientLanguage? language, out T row) where T : struct, IExcelSubrow<T> {
        if (!GetSubrowSheet<T>(data, language ?? Svc.ClientState.ClientLanguage).TryGetRow(rowId, out var rows) || subRowIndex < rows.Count) {
            row = default;
            return false;
        }

        row = rows[subRowIndex];
        return true;
    }

    public static bool TryFindSubrow<T>(this IDataManager data, Predicate<T> predicate, out T subrow) where T : struct, IExcelSubrow<T>
        => TryFindSubrow(data, predicate, null, out subrow);

    public static bool TryFindSubrow<T>(this IDataManager data, Predicate<T> predicate, ClientLanguage? language, out T subrow) where T : struct, IExcelSubrow<T> {
        foreach (var irow in GetSubrowSheet<T>(data, language ?? Svc.ClientState.ClientLanguage)) {
            foreach (var isubrow in irow) {
                if (predicate(isubrow)) {
                    subrow = isubrow;
                    return true;
                }
            }
        }

        subrow = default;
        return false;
    }

    // RawRow

    public static bool TryGetRawRow(this IDataManager data, string sheetName, uint rowId, out RawRow rawRow)
        => TryGetRow(data, sheetName, rowId, out rawRow);

    public static bool TryGetRawRow(this IDataManager data, string sheetName, uint rowId, ClientLanguage? language, out RawRow rawRow)
        => TryGetRow(data, sheetName, rowId, language, out rawRow);
}
