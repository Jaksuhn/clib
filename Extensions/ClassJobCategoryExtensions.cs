using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static class ClassJobCategoryExtensions {
    /// <summary>
    /// Checks that all jobs in a given <see cref="ClassJobCategory"/> are at or above a given level
    /// </summary>
    public static bool HasJobsAtLevel(this ClassJobCategory category, int level) {
        foreach (var cj in Svc.Data.GetSheet<ClassJob>()) {
            if (category.ExcelPage.ReadBool(category.RowOffset + cj.RowId + 4) && Svc.PlayerState.GetClassJobLevel(cj) < level)
                return false;
        }
        return true;
    }
}
