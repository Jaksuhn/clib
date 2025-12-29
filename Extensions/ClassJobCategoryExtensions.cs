using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static class ClassJobCategoryExtensions {
    public static bool HasJobAtLevel(this ClassJobCategory category, int level) {
        foreach (var cj in Svc.Data.GetSheet<ClassJob>()) {
            if (category.ExcelPage.ReadBool(category.RowOffset + cj.RowId + 4) && Svc.PlayerState.GetClassJobLevel(cj) < level)
                return false;
        }
        return true;
    }
}
