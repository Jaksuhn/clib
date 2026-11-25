using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;
using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static unsafe class AgentWorldTravelExtensions {
    public static void Travel(ref this AgentWorldTravel instance, string world) {
        if (ushort.TryParse(world, out var id))
            Travel(ref instance, id);
        else if (IDataManager.Get().FindRow<World>(r => r.Name.ToString().Contains(world, StringComparison.OrdinalIgnoreCase)) is { RowId: var rowId })
            Travel(ref instance, (ushort)rowId);
    }
    public static void Travel(ref this AgentWorldTravel instance, ushort destinationWorldId) {
        if (!TerritoryType.GetRow(IClientState.Get().TerritoryType).IsWorldTravelTerritory) return;
        if (World.GetRowOrNull(destinationWorldId) is not { IsOnCurrentDataCenter: true }) return;

        instance.DestinationWorldId = destinationWorldId;
        instance.SetupWorldTravelInfo((ushort)IPlayerState.Get().CurrentWorld.RowId, destinationWorldId);
        var retval = new AtkValue();
        Span<AtkValue> values = [new AtkValue { Type = AtkValueType.Int, Int = 0 }];
        instance.ReceiveEvent(&retval, values.GetPointer(0), (uint)values.Length, 1);
    }
}
