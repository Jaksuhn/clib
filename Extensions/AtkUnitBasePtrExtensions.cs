using Dalamud.Game.NativeWrapper;
using FFXIVClientStructs.FFXIV.Component.GUI;
using System;
using System.Collections.Generic;
using System.Text;

namespace clib.Extensions;

public static class AtkUnitBasePtrExtensions {
    public static unsafe AtkUnitBase* Struct(this AtkUnitBasePtr wrapper) => (AtkUnitBase*)wrapper.Address;
}
