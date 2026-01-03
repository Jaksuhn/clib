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
            { SoulCrystal: 1 } => InventoryType.ArmorySoulCrystal,
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null)
        };

        /// <summary>
        /// The slot index for <see cref="InventoryType.EquippedItems"/>
        /// </summary>
        public uint EquipSlot => item.EquipSlotCategory.Value switch {
            { MainHand: 1 } => 0,
            { OffHand: 1 } => 1,
            { Head: 1 } => 2,
            { Body: 1 } => 3,
            { Gloves: 1 } => 4,
            { Waist: 1 } => 5,
            { Legs: 1 } => 6,
            { Feet: 1 } => 7,
            { Ears: 1 } => 8,
            { Neck: 1 } => 9,
            { Wrists: 1 } => 10,
            { FingerL: 1 } => 11,
            { FingerR: 1 } => 12,
            { SoulCrystal: 1 } => 13,
            _ => throw new ArgumentOutOfRangeException(nameof(item), item, null)
        };

        public ItemHandle Handle => (ItemHandle)item;
    }
}
