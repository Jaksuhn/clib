using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
namespace clib.Services;

public sealed unsafe class ArmoireService : IDisposable {
    public event Action? ArmoireChanged;

    private Dictionary<uint, Sheets.Cabinet> _cabinetByItemId = [];
    private readonly HashSet<uint> _ownedItemIds = [];

    public ArmoireService() {
        LoadReverseCabinetMap();

        Svc.ClientState.Login += OnLogin;
        Svc.ClientState.Logout += OnLogout;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRefresh, "Cabinet", OnCabinetRefresh);

        if (Svc.ClientState.IsLoggedIn)
            RefreshCache();
    }

    public void Dispose() {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRefresh, "Cabinet", OnCabinetRefresh);
        Svc.ClientState.Logout -= OnLogout;
        Svc.ClientState.Login -= OnLogin;

        _ownedItemIds.Clear();
    }

    public void RefreshCache() {
        LoadReverseCabinetMap();
        BuildCache();
    }

    public HashSet<uint> GetArmoireItems() => [.. _ownedItemIds];
    public Sheets.Cabinet? GetCabinetRow(uint itemId) {
        LoadReverseCabinetMap();
        return _cabinetByItemId.TryGetValue(itemId, out var row) ? row : null;
    }

    private void OnLogin() => RefreshCache();
    private void OnLogout(int _, int __) => ClearCache();

    private void ClearCache() {
        var hadAny = _ownedItemIds.Count > 0;
        _ownedItemIds.Clear();
        if (hadAny)
            ArmoireChanged?.Invoke();
    }

    private void OnCabinetRefresh(AddonEvent _, AddonArgs __) => BuildCache();

    private void BuildCache() {
        if (!Svc.ClientState.IsLoggedIn) {
            ClearCache();
            return;
        }

        var uiState = UIState.Instance();
        if (uiState is null || !uiState->Cabinet.IsCabinetLoaded())
            return;

        var nextOwned = new HashSet<uint>();
        foreach (var (itemId, cabinetRow) in _cabinetByItemId) {
            if (uiState->Cabinet.IsItemInCabinet(cabinetRow.RowId))
                nextOwned.Add(itemId);
        }

        var changed = !_ownedItemIds.SetEquals(nextOwned);
        _ownedItemIds.Clear();
        _ownedItemIds.UnionWith(nextOwned);

        if (changed)
            ArmoireChanged?.Invoke();
    }

    private void LoadReverseCabinetMap() {
        if (_cabinetByItemId.Count > 0)
            return;

        _cabinetByItemId = Sheets.Cabinet
            .Where(x => x.RowId > 0 && x.Item.RowId > 0)
            .GroupBy(x => x.Item.RowId)
            .ToDictionary(g => g.Key, g => g.First());
    }
}
