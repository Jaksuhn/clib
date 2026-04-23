using Dalamud.Game.ClientState.Statuses;

namespace clib.Extensions;

public static class StatusListExtensions {
    private static readonly uint[] TwistOfFateStatusIDs = [1288, 1289];

    extension(StatusList list) {
        public bool HasTwistOfFate() => list.Any(s => TwistOfFateStatusIDs.Contains(s.StatusId));
    }
}
