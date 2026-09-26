using Dalamud.Game.Text.SeStringHandling.Payloads;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Lumina.Excel.Sheets;
using System.Text;
using System.Text.RegularExpressions;

namespace clib.Extensions;

public static unsafe partial class AddonAreaMapExtensions {
    public static Vector2? GetMouseWorldCoords(ref this AddonAreaMap areaMap)
        => MapToWorld(areaMap.MouseCoords, Map.GetRowRef(AgentMap.Instance()->SelectedMapId).Value);

    private static Vector2 MapToWorld(Vector2 pos, Map zone) {
        MapLinkPayload maplink = new(zone.TerritoryType.Value.RowId, zone.RowId, pos.X, pos.Y);
        return new(maplink.RawX / 1000f, maplink.RawY / 1000f);
    }
}
