using System.Collections.Frozen;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;

namespace clib.Services;

internal sealed unsafe class ArmoireService : IDisposable {
    public event Action? Changed;

    private static readonly Lazy<FrozenDictionary<uint, uint>> CabinetByItemId = new(()
        => Sheets.Cabinet.Where(row => row.Item.RowId != 0)
            .ToFrozenDictionary(row => row.Item.RowId, row => row.RowId));

    private static readonly Lazy<FrozenDictionary<uint, uint>> CabinetByRowId = new(()
        => Sheets.Cabinet.Where(row => row.RowId > 0 && row.Item.RowId != 0)
            .ToFrozenDictionary(row => row.RowId, row => row.Item.RowId));

    private Dictionary<uint, Sheets.Cabinet> _cabinetByItemId = [];
    private readonly HashSet<uint> _ownedItemIds = [];

    public ArmoireService() {
        LoadReverseCabinetMap();

        Svc.ClientState.Login += OnLogin;
        Svc.ClientState.Logout += OnLogout;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "Cabinet", OnCabinetRefresh);

        if (Svc.ClientState.IsLoggedIn)
            RefreshCache();
    }

    public void Dispose() {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "Cabinet", OnCabinetRefresh);
        Svc.ClientState.Logout -= OnLogout;
        Svc.ClientState.Login -= OnLogin;

        _ownedItemIds.Clear();
    }

    public void RefreshCache() {
        if (!LoadReverseCabinetMap()) {
            Svc.Log.Debug($"[{nameof(ArmoireService)}] Refreshing cabinet.");
            GameMain.ExecuteCommand((int)CommandFlag.RequestCabinet);
        }
        BuildCache(notify: true);
    }

    public HashSet<uint> GetArmoireItems() {
        BuildCache(notify: false);
        return [.. _ownedItemIds];
    }

    public Sheets.Cabinet? GetCabinetRow(uint itemId) {
        LoadReverseCabinetMap();
        return _cabinetByItemId.TryGetValue(itemId, out var row) ? row : null;
    }

    public bool IsCabinetItem(uint itemId)
        => ItemUtil.GetBaseId(itemId).ItemId is var baseId and not 0 && CabinetByItemId.Value.ContainsKey(baseId);

    public bool IsInArmoire(uint itemId) {
        itemId = ResolveCabinetItemId(itemId);
        if (itemId == 0)
            return false;
        BuildCache(notify: false);
        return _ownedItemIds.Contains(itemId) || IsInCabinet(itemId);
    }

    public uint ResolveCabinetItemId(uint cacheOrEntryId) {
        if (cacheOrEntryId == 0)
            return 0;
        var baseId = ItemUtil.GetBaseId(cacheOrEntryId).ItemId;
        if (CabinetByItemId.Value.ContainsKey(baseId))
            return baseId;
        if (CabinetByRowId.Value.TryGetValue(cacheOrEntryId, out var fromEntry))
            return ItemUtil.GetBaseId(fromEntry).ItemId;
        if (CabinetByRowId.Value.TryGetValue(baseId, out var fromBase))
            return ItemUtil.GetBaseId(fromBase).ItemId;
        return baseId;
    }

    public bool IsInCabinet(uint itemId) {
        itemId = ItemUtil.GetBaseId(itemId).ItemId;
        if (!CabinetByItemId.Value.TryGetValue(itemId, out var cabinetRowId))
            return false;

        var uiState = UIState.Instance();
        var liveInCabinet = uiState is not null && uiState->Cabinet.IsCabinetLoaded() && uiState->Cabinet.IsItemInCabinet(cabinetRowId);

        var bitsetInCabinet = false;
        var itemFinderModule = ItemFinderModule.Instance();
        if (itemFinderModule is not null) {
            var (byteIndex, bitOffset) = Math.DivRem(cabinetRowId - 1048, 32u);
            if (itemFinderModule->CabinetItemUnlockBits.Length > byteIndex)
                bitsetInCabinet = (itemFinderModule->CabinetItemUnlockBits[(int)byteIndex] & (1 << (int)bitOffset)) != 0;
        }

        return liveInCabinet || bitsetInCabinet;
    }

    private void OnLogin() {
        Svc.Log.Debug($"[{nameof(ArmoireService)}] Refreshing cabinet.");
        GameMain.ExecuteCommand((int)CommandFlag.RequestCabinet);
        RefreshCache();
    }

    private void OnLogout(int _, int __) => ClearCache();

    private void ClearCache() {
        var hadAny = _ownedItemIds.Count > 0;
        _ownedItemIds.Clear();
        if (hadAny)
            Changed?.Invoke();
    }

    private void OnCabinetRefresh(AddonEvent _, AddonArgs __) => BuildCache(notify: true);

    private void BuildCache(bool notify) {
        if (!Svc.ClientState.IsLoggedIn) {
            ClearCache();
            return;
        }

        var nextOwned = new HashSet<uint>();
        foreach (var (itemId, cabinetRow) in _cabinetByItemId) {
            if (IsInCabinet(itemId))
                nextOwned.Add(ItemUtil.GetBaseId(itemId).ItemId);
        }

        var changed = !_ownedItemIds.SetEquals(nextOwned);
        _ownedItemIds.Clear();
        _ownedItemIds.UnionWith(nextOwned);

        if (notify && changed) {
            Svc.Log.Debug($"[{nameof(ArmoireService)}] Cabinet changed.");
            Changed?.Invoke();
        }
    }

    private bool LoadReverseCabinetMap() {
        if (_cabinetByItemId.Count > 0)
            return false;

        _cabinetByItemId = Sheets.Cabinet.Where(x => x.RowId > 0 && x.Item.RowId > 0).GroupBy(x => x.Item.RowId).ToDictionary(g => g.Key, g => g.First());
        return true;
    }
}
