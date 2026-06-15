using clib.Services;

namespace clib.Extensions;

public static class IGameGuiExtensions {
    public static unsafe bool TryGetAddon<T>(string name, out T* AddonPtr) where T : unmanaged {
        if (Svc.GameGui.GetAddonByName(name) is { } ptr) {
            AddonPtr = (T*)ptr.Address;
            return true;
        }
        AddonPtr = null;
        return false;
    }
}
