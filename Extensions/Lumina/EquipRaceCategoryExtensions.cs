using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static unsafe class EquipRaceCategoryExtensions {
    extension(EquipRaceCategory row) {
        public Collection<bool> Races
            => new(row.ExcelPage, parentOffset: row.RowOffset, offset: row.RowOffset, &RaceCtor, size: row.ExcelPage.Module.GetSheet<Race>().Count);

        public Collection<bool> Sexes
            => new(row.ExcelPage, parentOffset: row.RowOffset, offset: row.RowOffset + (uint)row.ExcelPage.Module.GetSheet<Race>().Count, &SexCtor, size: 2);

        public bool CanEquip => get_Races(row)[PlayerState.Instance()->Race] && get_Sexes(row)[PlayerState.Instance()->Sex];
    }

    private static bool RaceCtor(ExcelPage page, uint parentOffset, uint offset, uint i)
        => page.ReadBool(offset + i);

    private static bool SexCtor(ExcelPage page, uint parentOffset, uint offset, uint i)
        => page.ReadPackedBool(offset, (byte)i);
}
