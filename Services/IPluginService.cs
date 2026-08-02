namespace clib.Services;

/// <summary>
/// Marker for plugin singletons auto-registered in <see cref="CLibMain.Init"/>.
/// Disposed in <see cref="CLibMain.Dispose"/> / <see cref="CLibMain.DisposeAsync"/>.
/// Types that also implement <c>IPluginConfiguration</c> are loaded via
/// <c>GetPluginConfig()</c> when a saved config exists.
/// </summary>
public interface IPluginService {
    /// <remarks>Initialises in ascending order.</remarks>
    int InitOrder => 0;
}
