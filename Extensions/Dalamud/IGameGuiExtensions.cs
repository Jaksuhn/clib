using clib.Services;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace clib.Extensions;

public static class IGameGuiExtensions {
    extension(IGameGui gui) {
        public unsafe bool TryGetAddon<T>(string name, out T* AddonPtr) where T : unmanaged {
            if (Svc.GameGui.GetAddonByName(name) is { Address: var addr }) {
                AddonPtr = (T*)addr;
                return ((AtkUnitBase*)AddonPtr)->IsAddonReady();
            }
            AddonPtr = null;
            return false;
        }
    }
}
