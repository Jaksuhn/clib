using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using InteropGenerator.Runtime;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static unsafe class MirageStoreSetItemExtensions {
    public const int ItemCount = 11;

    extension(MirageStoreSetItem row) {
        public RowRef<Item> Set => new(row.ExcelPage.Module, row.RowId, row.ExcelPage.Language);
        public Collection<RowRef<Item>> Items => new(row.ExcelPage, parentOffset: row.RowOffset, offset: row.RowOffset, &ItemCtor, size: 11);

        // https://github.com/Haselnussbomber/HaselCommon/blob/main/HaselCommon/Extensions/Sheets/MirageStoreSetItemExtensions.cs
        public bool TryGetSetItemBitArray(out BitArray bitArray, bool useCache = true) {
            var mirageManager = MirageManager.Instance();
            if (mirageManager->PrismBoxLoaded) {
                var prismBoxItemIndex = mirageManager->PrismBoxItemIds.IndexOf(row.RowId);
                if (prismBoxItemIndex != -1) {
                    bitArray = new BitArray(mirageManager->PrismBoxStain0Ids.GetPointer(prismBoxItemIndex), ItemCount);
                    return true;
                }
            }

            if (useCache) {
                var itemFinderModule = ItemFinderModule.Instance();
                var glamourDresserIndex = itemFinderModule->GlamourDresserItemIds.IndexOf(row.RowId);
                if (glamourDresserIndex != -1) {
                    bitArray = new BitArray((byte*)itemFinderModule->GlamourDresserItemSetUnlockBits.GetPointer(glamourDresserIndex), ItemCount);
                    return true;
                }
            }

            bitArray = default;
            return false;
        }
    }

    internal static RowRef<Item> ItemCtor(ExcelPage page, uint parentOffset, uint offset, uint i)
        => new(page.Module, page.ReadUInt32(offset + i * 4), page.Language);
}
