using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace clib.Services;

public sealed unsafe class ArmoireService(IReadOnlyDictionary<uint, uint> cabinetRowIdByItemId) : IDisposable {
    private readonly Dictionary<uint, uint> _cabinetRowIdByItemId = cabinetRowIdByItemId.ToDictionary(kv => kv.Key, kv => kv.Value);
    private readonly HashSet<uint> _ownedItemIds = [];
    private bool _enabled;
    private bool _refreshRequested;
    private bool _cabinetLoadRequested;

    public event Action? OwnershipChanged;

    public void Enable() {
        if (_enabled)
            return;
        _enabled = true;

        Svc.ClientState.Login += OnLogin;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Cabinet", OnCabinetRefresh);
        Svc.Framework.Update += OnFrameworkUpdate;

        if (Svc.ClientState.IsLoggedIn)
            RequestRefresh();
    }

    public void Disable() {
        if (!_enabled)
            return;
        _enabled = false;

        Svc.Framework.Update -= OnFrameworkUpdate;
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "Cabinet", OnCabinetRefresh);
        Svc.ClientState.Login -= OnLogin;
    }

    public void Dispose() {
        Disable();
        _ownedItemIds.Clear();
    }

    public void RequestRefresh() {
        _refreshRequested = true;
        _cabinetLoadRequested = false;
    }

    public HashSet<uint> GetOwnedItemIds() => [.. _ownedItemIds];

    private void OnLogin() => RequestRefresh();

    private void ClearOwnedSnapshot() {
        var hadAny = _ownedItemIds.Count > 0;
        _ownedItemIds.Clear();
        _refreshRequested = false;
        _cabinetLoadRequested = false;
        if (hadAny)
            OwnershipChanged?.Invoke();
    }

    private void OnCabinetRefresh(AddonEvent _, AddonArgs __) => RequestRefresh();

    private void OnFrameworkUpdate(IFramework _) {
        if (!_enabled)
            return;

        if (!Svc.ClientState.IsLoggedIn) {
            ClearOwnedSnapshot();
            return;
        }

        if (!_refreshRequested)
            return;

        if (!_cabinetLoadRequested) {
            // User-confirmed command id for requesting cabinet data load.
            GameMain.ExecuteCommand(423);
            _cabinetLoadRequested = true;
            return;
        }

        var uiState = UIState.Instance();
        if (uiState is null || !uiState->Cabinet.IsCabinetLoaded())
            return;

        var nextOwned = new HashSet<uint>();
        foreach (var (itemId, cabinetRowId) in _cabinetRowIdByItemId) {
            if (uiState->Cabinet.IsItemInCabinet(cabinetRowId))
                nextOwned.Add(itemId);
        }

        var changed = !_ownedItemIds.SetEquals(nextOwned);
        _ownedItemIds.Clear();
        _ownedItemIds.UnionWith(nextOwned);
        _refreshRequested = false;

        if (changed)
            OwnershipChanged?.Invoke();
    }
}
