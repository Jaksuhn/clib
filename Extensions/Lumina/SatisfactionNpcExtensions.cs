using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static class SatisfactionNpcExtensions {
    extension(SatisfactionNpc row) {
        public RowRef<Achievement> Achievement
            => new(row.ExcelPage.Module, Achievement.FirstOrNull(r => r is { AchievementCategory.RowId: 22, AchievementTarget.RowId: 35, Data: [{ RowId: 150 }, ..] } && r.Description.ContainsText(row.Npc.Value.Singular))?.RowId ?? new());
    }
}
