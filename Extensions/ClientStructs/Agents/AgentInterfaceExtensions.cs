using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace clib.Extensions;

public static class AgentInterfaceExtensions {
    extension(ref AgentInterface agentInterface) {
        public unsafe void CloseAddon(bool fireCallback = true) {
            var addon = RaptureAtkUnitManager.Instance()->GetAddonById((ushort)agentInterface.AddonId);
            if (addon != null && addon->IsVisible)
                addon->Close(fireCallback);
        }
    }
}
