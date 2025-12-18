namespace clib.Enums;

/// <summary>
/// For <see cref="CommandFlag.RepairAllItems"/> and <seealso cref="CommandFlag.RepairAllItemsNPC"/>
/// </summary>
public enum RepairCategory : int {
    MainOffHand = 0,
    HeadBodyArms = 1,
    LegsFeet = 2,
    EarsNeck = 3,
    WristRing = 4,
    Inventory = 5,
}
