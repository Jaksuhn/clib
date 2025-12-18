using Dalamud.Game.Inventory;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.FFXIV.Component.Excel;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using ValueType = FFXIVClientStructs.FFXIV.Component.GUI.ValueType;

namespace clib.Utils;

public class ItemWrapper {
    public ItemWrapper(uint itemId) {
        ItemId = itemId;
        unsafe {
            ExcelRow = ItemUtil.IsEventItem(ItemId) ? Framework.Instance()->ExcelModuleInterface->ExdModule->GetRowBySheetIndexAndRowIndex(124, ItemId)
                : Framework.Instance()->ExcelModuleInterface->ExdModule->GetRowBySheetIndexAndRowIndex(10, ItemUtil.GetBaseId(ItemId).ItemId);
        }
    }

    public ItemWrapper(ItemLocation itemLocation) {
        ItemLocation = itemLocation;
        unsafe {
            var inventoryItem = itemLocation.GetInventoryItem();
            ItemId = inventoryItem != null ? inventoryItem->GetItemId() : 0;
            ExcelRow = ItemUtil.IsEventItem(ItemId) ? Framework.Instance()->ExcelModuleInterface->ExdModule->GetRowBySheetIndexAndRowIndex(124, ItemId)
                : Framework.Instance()->ExcelModuleInterface->ExdModule->GetRowBySheetIndexAndRowIndex(10, ItemUtil.GetBaseId(ItemId).ItemId);
        }
    }

    public static unsafe implicit operator ItemWrapper(InventoryItem* item) => new(item->ItemId);
    public static implicit operator ItemWrapper(InventoryItem item) => new(item.ItemId);
    public static implicit operator ItemWrapper(GameInventoryItem item) => new(item.ItemId);
    public static implicit operator ItemWrapper(Item item) => new(item.RowId);
    public static implicit operator ItemWrapper(RowRef<Item> rowRef) => new(rowRef.RowId);
    public static implicit operator ItemWrapper(EventItem eventItem) => new(eventItem.RowId);
    public static implicit operator ItemWrapper(RowRef<EventItem> rowRef) => new(rowRef.RowId);
    public static implicit operator ItemWrapper(ItemLocation itemLocation) => new(itemLocation);
    public static implicit operator ItemWrapper(uint itemId) => new(itemId);
    public static implicit operator uint(ItemWrapper itemInfo) => itemInfo.ItemId;

    public uint ItemId { get; }
    public ItemLocation? ItemLocation { get; }
    public unsafe ExcelRow* ExcelRow { get; }

    public RowRef<Item> GameData => Svc.Data.GetRef<Item>(ItemId);
    public bool IsValid => ItemId is not 0;

    public uint BaseItemId => ItemUtil.GetBaseId(ItemId).ItemId;
    public ItemKind ItemKind => ItemUtil.GetBaseId(ItemId).Kind;
    public bool IsNormalItem => ItemUtil.IsNormalItem(ItemId);
    public bool IsCollectible => ItemUtil.IsCollectible(ItemId);
    public bool IsHighQuality => ItemUtil.IsHighQuality(ItemId);
    public bool IsEventItem => ItemUtil.IsEventItem(ItemId);

    public unsafe bool InGearset {
        get {
            var gm = RaptureGearsetModule.Instance();
            for (byte i = 0; i < 100; ++i) {
                if (!gm->IsValidGearset(i)) continue;
                var gearset = gm->GetGearset(i);
                if (gearset != null && gearset->Flags.HasFlag(RaptureGearsetModule.GearsetFlag.Exists))
                    if (gearset->Items.ToArray().Any(x => ItemUtil.GetBaseId(x.ItemId).ItemId == ItemId)) return true;
            }
            return false;
        }
    }

    public unsafe int GetCount(bool ignoreHq = true)
        => ignoreHq ? InventoryManager.Instance()->GetInventoryItemCount(BaseItemId) + InventoryManager.Instance()->GetInventoryItemCount(ItemId, true)
        : InventoryManager.Instance()->GetInventoryItemCount(ItemId);

    public unsafe bool CanEquip(out RowRef<LogMessage> errorMsg) {
        var logMessageId = InventoryManager.CanEquip(ItemId,
        PlayerState.Instance()->Race,
        PlayerState.Instance()->Sex,
        PlayerState.Instance()->GetClassJobLevel(-1, false),
        PlayerState.Instance()->CurrentClassJobId,
        PlayerState.Instance()->GrandCompany,
        PvPProfile.Instance()->GetPvPRank(),
        ExcelRow);
        errorMsg = Svc.Data.GetRef<LogMessage>((uint)logMessageId);
        return logMessageId is 0;
    }

    /// <summary>
    /// Be sure to check <see cref="CanEquip"/> first. This only handles the move operation
    /// </summary>
    /// <param name="equipRingR">Rings will be equipped in the L slot unless specified</param>
    public unsafe void Equip(bool equipRingR = false) {
        if (ItemLocation is null) return;
        if (GetContainerId(ItemLocation.Container) is not 0 and var srcContId && GetContainerId(InventoryType.EquippedItems) is not 0 and var destContId) {
            var eis = stackalloc AtkValue[4];
            var dropOut = stackalloc AtkValue[32];
            for (var i = 0; i < 4; i++) eis[i].Type = ValueType.UInt;
            eis[0].UInt = srcContId;
            eis[1].UInt = ItemLocation.Slot;
            eis[2].UInt = destContId;
            eis[3].UInt = equipRingR ? 12 : GetEquipSlot(GameData.Value.ArmouryContainer);
            RaptureAtkModule.Instance()->HandleItemMove(dropOut, eis, 4);
        }

        static uint GetContainerId(InventoryType inventoryType) => inventoryType switch {
            InventoryType.Inventory1 => 48,
            InventoryType.Inventory2 => 49,
            InventoryType.Inventory3 => 50,
            InventoryType.Inventory4 => 51,
            InventoryType.ArmoryMainHand => 57,
            InventoryType.ArmoryHead => 58,
            InventoryType.ArmoryBody => 59,
            InventoryType.ArmoryHands => 60,
            InventoryType.ArmoryLegs => 61,
            InventoryType.ArmoryFeets => 62,
            InventoryType.ArmoryOffHand => 63,
            InventoryType.ArmoryEar => 64,
            InventoryType.ArmoryNeck => 65,
            InventoryType.ArmoryWrist => 66,
            InventoryType.ArmoryRings => 67,
            InventoryType.ArmorySoulCrystal => 68,
            InventoryType.EquippedItems => 4,
            _ => 0
        };

        static uint GetEquipSlot(InventoryType armouryContainer) => armouryContainer switch {
            InventoryType.ArmoryMainHand => 0,
            InventoryType.ArmoryOffHand => 1,
            InventoryType.ArmoryHead => 2,
            InventoryType.ArmoryBody => 3,
            InventoryType.ArmoryHands => 4,
            InventoryType.ArmoryWaist => 5,
            InventoryType.ArmoryLegs => 6,
            InventoryType.ArmoryFeets => 7,
            InventoryType.ArmoryEar => 8,
            InventoryType.ArmoryNeck => 9,
            InventoryType.ArmoryWrist => 10,
            InventoryType.ArmoryRings => 11, // 11 = L, 12 = R
            InventoryType.ArmorySoulCrystal => 13,
            _ => throw new ArgumentOutOfRangeException($"Container [{armouryContainer}] is not a valid Armoury container"),
        };
    }

    public override string ToString() => IsValid ? $"[#{ItemId}] {GameData.Value.Name}" : $"{nameof(ItemWrapper)}#Invalid";
}
