using System.Collections.Frozen;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using FFXIVClientStructs.Interop;
using InteropGenerator.Runtime;
using static FFXIVClientStructs.FFXIV.Client.Game.UI.Cabinet;

namespace clib.Services;

// Ownership checks follow HaselCommon CabinetService (live when Loaded, else ItemFinder bitset).
// https://github.com/Haselnussbomber/HaselCommon/blob/main/HaselCommon/Services/CabinetService.cs
internal sealed unsafe class ArmoireService : IDisposable {
    public event Action? Changed;

    private static readonly Lazy<FrozenDictionary<uint, uint>> CabinetByItemId = new(()
        => Sheets.Cabinet.Where(row => row.Item.RowId != 0)
            .ToFrozenDictionary(row => row.Item.RowId, row => row.RowId));

    private static readonly Lazy<FrozenDictionary<uint, uint>> CabinetByRowId = new(()
        => Sheets.Cabinet.Where(row => row.RowId > 0 && row.Item.RowId != 0)
            .ToFrozenDictionary(row => row.RowId, row => row.Item.RowId));

    private static readonly Lazy<int> CabinetRowCount = new(() => Svc.Data.GetExcelSheet<Sheets.Cabinet>()!.Count);

    private readonly HashSet<uint> _ownedItemIds = [];

    public ArmoireService() {
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
        Svc.Log.Debug($"[{nameof(ArmoireService)}] Refreshing cabinet.");
        GameMain.ExecuteCommand((int)CommandFlag.RequestCabinet);
        BuildCache(notify: true);
    }

    public HashSet<uint> GetArmoireItems() {
        BuildCache(notify: false);
        return [.. _ownedItemIds];
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

    public bool IsInCabinet(uint itemId, bool useCache = true) {
        itemId = ItemUtil.GetBaseId(itemId).ItemId;
        if (!CabinetByItemId.Value.TryGetValue(itemId, out var cabinetRowId))
            return false;
        return IsCabinetItemCollected(cabinetRowId, useCache);
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

    // https://github.com/Haselnussbomber/HaselCommon/blob/a411775f6e97b0c82fc81a66ae918a2027c709d3/HaselCommon/Services/CabinetService.cs#L36-L53
    private static bool IsCabinetItemCollected(uint cabinetId, bool useCache = true) {
        var uiState = UIState.Instance();
        if (uiState is null)
            return false;

        ref var cabinet = ref uiState->Cabinet;
        if (cabinet.State == CabinetState.Loaded)
            return cabinet.IsItemInCabinet(cabinetId);

        if (!useCache)
            return false;

        var itemFinderModule = ItemFinderModule.Instance();
        if (itemFinderModule is null)
            return false;

        var bitArray = new BitArray((byte*)itemFinderModule->CabinetItemUnlockBits.GetPointer(0), CabinetRowCount.Value);
        return bitArray.Get((int)cabinetId);
    }

    private void OnLogin() => RefreshCache();

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
        foreach (var (itemId, cabinetRowId) in CabinetByItemId.Value) {
            if (IsCabinetItemCollected(cabinetRowId))
                nextOwned.Add(itemId);
        }

        var changed = !_ownedItemIds.SetEquals(nextOwned);
        _ownedItemIds.Clear();
        _ownedItemIds.UnionWith(nextOwned);

        if (notify && changed) {
            Svc.Log.Debug($"[{nameof(ArmoireService)}] Cabinet changed.");
            Changed?.Invoke();
        }
    }
}
