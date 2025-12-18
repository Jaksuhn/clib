using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace clib.Extensions;

public static class InventoryTypeExtensions {
    public static unsafe InventoryContainer* GetContainer(this InventoryType inv) => InventoryManager.Instance()->GetInventoryContainer(inv);
    public static unsafe ItemOrderModuleSorter* GetSorter(this InventoryType inv) {
        var m = ItemOrderModule.Instance();
        var sorter = inv switch {
            InventoryType.ArmoryMainHand => m->ArmouryMainHandSorter,
            InventoryType.ArmoryHead => m->ArmouryHeadSorter,
            InventoryType.ArmoryBody => m->ArmouryBodySorter,
            InventoryType.ArmoryHands => m->ArmouryHandsSorter,
            InventoryType.ArmoryLegs => m->ArmouryLegsSorter,
            InventoryType.ArmoryFeets => m->ArmouryFeetSorter,
            InventoryType.ArmoryOffHand => m->ArmouryOffHandSorter,
            InventoryType.ArmoryEar => m->ArmouryEarsSorter,
            InventoryType.ArmoryNeck => m->ArmouryNeckSorter,
            InventoryType.ArmoryWrist => m->ArmouryWristsSorter,
            InventoryType.ArmoryRings => m->ArmouryRingsSorter,
            InventoryType.ArmorySoulCrystal => m->ArmourySoulCrystalSorter,
            InventoryType.SaddleBag1 or InventoryType.SaddleBag2 => m->SaddleBagSorter,
            InventoryType.PremiumSaddleBag1 or InventoryType.PremiumSaddleBag2 => m->PremiumSaddleBagSorter,
            InventoryType.Inventory1 or InventoryType.Inventory2 or InventoryType.Inventory3 or InventoryType.Inventory4 => m->InventorySorter,
            _ => null
        };
        return sorter;
    }
}
