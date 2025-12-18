using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace clib.Extensions;

public static unsafe class AddonArgsExtensions {
    extension(AddonArgs args) {
        public unsafe AtkEventListener* EventListener => &args.GetAddon<AtkUnitBase>()->AtkEventListener;
    }

    public static T* GetAddon<T>(this AddonArgs args) where T : unmanaged => (T*)args.Addon.Address;
}
