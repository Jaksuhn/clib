using clib.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace clib.Internal;

// don't keep hooks in this lib for long
internal class Hooks : IDisposable {
    internal unsafe delegate bool CabinetRequestDelegate(Cabinet* cabinet);
    internal readonly CabinetRequestDelegate CabinetRequest;

    public Hooks() {
        CabinetRequest = Svc.Hook.GetDelegate<CabinetRequestDelegate>("48 83 EC 38 C7 01 ?? ?? ?? ??");
    }

    public void Dispose() { }
}
