using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace clib.Extensions;

public static class FlagMapMarkerExtensions {
    public static unsafe Vector3 ToVector3(this FlagMapMarker flag) => AgentMap.Instance()->FlagMarkerCount > 0 ? Svc.Navmesh.PointOnFloor(new(flag.XFloat, 1024, flag.YFloat)) ?? Vector3.NaN : Vector3.NaN;
}
