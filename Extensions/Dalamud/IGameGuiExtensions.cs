using clib.Services;

namespace clib.Extensions;

public static class IGameGuiExtensions {
    public static unsafe bool TryGetAddon<T>(string name, out T* AddonPtr) where T : unmanaged {
        if (Svc.GameGui.GetAddonByName(name) is { Address: var addr }) {
            AddonPtr = (T*)addr;
            return true;
        }
        AddonPtr = null;
        return false;
    }
}
