namespace clib.Services;

/// <summary>
/// Empty interface for declaring a plugin service. Any class that implements this will be automatically registered in <see cref="CLibMain.Init(Dalamud.Plugin.IDalamudPluginInterface, object, CLibModule)"/>.
/// And disposed in <see cref="CLibMain.Dispose"/>.
/// </summary>
public interface IPluginService {
    /// <remarks>Initialises in ascending order.</remarks>
    int InitOrder => 0;
}
