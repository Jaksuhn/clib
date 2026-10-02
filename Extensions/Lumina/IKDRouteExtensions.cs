using FFXIVClientStructs.FFXIV.Client.System.Framework;
using Lumina.Excel;
using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static class IKDRouteExtensions {
    extension(IKDRoute row) {
        public static RowRef<ContentFinderCondition> CurrentIndigo {
            get {
                var t = Framework.GetServerTime() - 57600;
                if (t % 7200 >= 900) return default;
                var idx = (uint)(t / 7200 % IKDRouteTable.Rows.Count());
                return IKDRouteTable.GetRow(idx).Route.Value.Instance; // Route = IndigoRoute
            }
        }

        public static RowRef<ContentFinderCondition> CurrentRuby {
            get {
                var t = Framework.GetServerTime() - 57600;
                if (t % 7200 >= 900) return default;
                var idx = (uint)(t / 7200 % IKDRouteTable.Rows.Count());
                return IKDRoute.GetRow(IKDRouteTable.GetRow(idx).Unknown0).Instance; // Unknown0 = RubyRoute
            }
        }
    }
}
