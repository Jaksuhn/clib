using clib.Services;
using Dalamud.Game.ClientState.Conditions;
using FFXIVClientStructs.FFXIV.Client.Game.Group;
using FFXIVClientStructs.FFXIV.Client.UI.Info;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace clib.Extensions;

public static unsafe class InfoProxyPartyInviteExtensions {
    public enum FailedInviteReason {
        None,
        GroupFull,
        NotPartyLead,
        TargetInParty,
    }

    extension(InfoProxyPartyInvite proxy) {
        public static bool IsInParty(ulong contentId) {
            if (contentId == 0) return false;
            if (InfoProxyCrossRealm.IsCrossRealmParty())
                return InfoProxyCrossRealm.GetMemberByContentId(contentId) != null;
            return IPartyList.Get().Any(p => p.ContentId == contentId);
        }

        public static bool CanInviteToParty(ulong contentId, [NotNullWhen(false)] out FailedInviteReason? reason) {
            var crossRealm = InfoProxyCrossRealm.IsCrossRealmParty();
            var memberCount = crossRealm ? InfoProxyCrossRealm.GetPartyMemberCount() : GroupManager.Instance()->GetGroup()->MemberCount;
            if (memberCount >= 8) {
                reason = FailedInviteReason.GroupFull;
                return false;
            }
            if (crossRealm ? !InfoProxyCrossRealm.IsLocalPlayerPartyLeader() : IPlayerState.Get() is { InParty: true, IsPartyLeader: false }) {
                reason = FailedInviteReason.NotPartyLead;
                return false;
            }
            if (InfoProxyPartyInvite.IsInParty(contentId)) {
                reason = FailedInviteReason.TargetInParty;
                return false;
            }
            reason = null;
            return true;
        }

        /// <summary>
        /// Check <see cref="CanInviteToParty(ulong, out FailedInviteReason?)"/> first.
        /// </summary>
        /// <param name="homeWorldId">Target's home world.</param>
        /// <param name="currentWorldId">Target's current world, if known. If it differs from local player, a cross-world invite is sent</param>
        public static bool Invite(ulong contentId, string playerName, ushort homeWorldId, ushort currentWorldId = 0) {
            var instance = InfoProxyPartyInvite.Instance();
            if (ICondition.Get()[ConditionFlag.BoundByDuty56]) {
                IPluginLog.Get().Print($"Inviting {playerName} to instanced party");
                return instance->InviteToPartyInInstanceByContentId(contentId);
            }

            var crossWorld = currentWorldId != 0 && currentWorldId != IPlayerState.Get().CurrentWorld.RowId;
            if (crossWorld || InfoProxyCrossRealm.IsCrossRealmParty()) {
                if (contentId == 0) {
                    IPluginLog.Get().Error($"Unable to invite {playerName} cross-realm: no content id");
                    return false;
                }
                // same world, in cross realm is technically a different function in the game but this one works just fine
                IPluginLog.Get().Print($"Inviting {playerName} cross-realm ({(crossWorld ? "other world" : "same world, in cross-realm party")})");
                return instance->InviteToPartyContentId(contentId, homeWorldId);
            }

            IPluginLog.Get().Print($"Inviting {playerName} to local party");
            fixed (byte* namePtr = ToTerminatedBytes(playerName))
                return instance->InviteToParty(contentId, namePtr, homeWorldId);
        }
    }

    private static byte[] ToTerminatedBytes(string s) {
        var utf8 = Encoding.UTF8;
        var bytes = new byte[utf8.GetByteCount(s) + 1];
        utf8.GetBytes(s, 0, s.Length, bytes, 0);
        bytes[^1] = 0;
        return bytes;
    }
}
