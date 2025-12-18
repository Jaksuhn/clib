using FFXIVClientStructs.FFXIV.Client.Game;
using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static class ItemExtensions {
    extension(Item item) {
        public InventoryType ArmouryContainer => item.EquipSlotCategory.Value switch {
            { MainHand: 1 } => InventoryType.ArmoryMainHand,
            { OffHand: 1 } => InventoryType.ArmoryOffHand,
            { Head: 1 } => InventoryType.ArmoryHead,
            { Body: 1 } => InventoryType.ArmoryBody,
            { Gloves: 1 } => InventoryType.ArmoryHands,
            { Legs: 1 } => InventoryType.ArmoryLegs,
            { Feet: 1 } => InventoryType.ArmoryFeets,
            { Ears: 1 } => InventoryType.ArmoryEar,
            { Neck: 1 } => InventoryType.ArmoryNeck,
            { Wrists: 1 } => InventoryType.ArmoryWrist,
            { FingerL: 1 } => InventoryType.ArmoryRings,
            { FingerR: 1 } => InventoryType.ArmoryRings,
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null)
        };
    }
}
