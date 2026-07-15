using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Game.Addon.Lifecycle.AddonArgTypes;
using Dalamud.Utility;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;

namespace clib.Services;

internal sealed unsafe class DresserService : IDisposable {
    public event System.Action? Changed;

    private readonly HashSet<uint> _dresserItemIds = [];

    public DresserService() {
        Svc.ClientState.Login += OnLogin;
        Svc.ClientState.Logout += OnLogout;
        Svc.AddonLifecycle.RegisterListener(AddonEvent.PostRequestedUpdate, "MiragePrismPrismBox", OnPrismBoxRefresh);

        if (Svc.ClientState.IsLoggedIn)
            RefreshCache();
    }

    public void Dispose() {
        Svc.AddonLifecycle.UnregisterListener(AddonEvent.PostRequestedUpdate, "MiragePrismPrismBox", OnPrismBoxRefresh);
        Svc.ClientState.Logout -= OnLogout;
        Svc.ClientState.Login -= OnLogin;
        _dresserItemIds.Clear();
    }

    public void RefreshCache() => BuildCache(notify: true);

    public HashSet<uint> GetDresserItemIds() {
        BuildCache(notify: false);
        return [.. _dresserItemIds];
    }

    public bool IsInDresserLoose(uint itemId, ISet<uint>? outfitTokenIds = null) {
        itemId = ItemUtil.GetBaseId(itemId).ItemId;
        if (itemId == 0)
            return false;
        var dresser = GetDresserItemIds();
        if (!dresser.Contains(itemId))
            return false;
        if (outfitTokenIds is null)
            return !IsMirageSetToken(itemId);
        return !outfitTokenIds.Contains(itemId);
    }

    public bool IsInOutfitSlot(uint pieceItemId, uint? setItemId = null) {
        pieceItemId = ItemUtil.GetBaseId(pieceItemId).ItemId;
        if (pieceItemId == 0)
            return false;

        if (setItemId is { } setId and not 0) {
            setId = ItemUtil.GetBaseId(setId).ItemId;
            var row = MirageStoreSetItem.GetRow(setId);
            return IsPieceInMirageOutfitSlot(row, pieceItemId);
        }

        return IsPieceInAnyMirageOutfitSlot(pieceItemId);
    }

    public bool IsFullyDepositedInDresser(uint itemId, ISet<uint>? outfitTokenIds = null) {
        itemId = ItemUtil.GetBaseId(itemId).ItemId;
        if (itemId == 0)
            return false;

        var dresser = GetDresserItemIds();
        if (dresser.Contains(itemId) && (outfitTokenIds is null ? !IsMirageSetToken(itemId) : !outfitTokenIds.Contains(itemId)))
            return true;

        var inAnyOutfit = false;
        foreach (var row in MirageStoreSetItem.Where(r => r.RowId > 0)) {
            if (!row.Items.Any(itemRef => itemRef.RowId != 0 && itemRef.RowId == itemId))
                continue;
            inAnyOutfit = true;
            if (!IsPieceInMirageOutfitSlot(row, itemId))
                return false;
        }
        return inAnyOutfit;
    }

    public static bool IsPieceInMirageOutfitSlot(MirageStoreSetItem row, uint pieceItemId)
        => row.Items.Select((itemRef, slotIndex) => (itemRef, slotIndex))
            .Any(x => x.itemRef.RowId != 0 && x.itemRef.RowId == pieceItemId && row.IsSetSlotCollected(x.slotIndex));

    public static bool IsPieceInAnyMirageOutfitSlot(uint pieceItemId)
        => MirageStoreSetItem.Where(r => r.RowId > 0).Any(r => IsPieceInMirageOutfitSlot(r, pieceItemId));

    private static bool IsMirageSetToken(uint itemId)
        => MirageStoreSetItem.TryGetRow(itemId, out var row) && row.RowId > 0;

    private void OnLogin() {
        Svc.Log.Debug($"[{nameof(DresserService)}] Requesting prism box.");
        GameMain.ExecuteCommand((int)CommandFlag.RequestPrismBox);
        RefreshCache();
    }

    private void OnLogout(int _, int __) => ClearCache();

    private void ClearCache() {
        var hadAny = _dresserItemIds.Count > 0;
        _dresserItemIds.Clear();
        if (hadAny)
            Changed?.Invoke();
    }

    private void OnPrismBoxRefresh(AddonEvent _, AddonArgs __) => BuildCache(notify: true);

    private void BuildCache(bool notify) {
        if (!Svc.ClientState.IsLoggedIn) {
            ClearCache();
            return;
        }

        var finder = ItemFinderModule.Instance();
        HashSet<uint> next;
        if (finder is null) {
            next = [];
        }
        else {
            next = [];
            foreach (var id in finder->GlamourDresserBaseItemIds) {
                if (id != 0)
                    next.Add(id);
            }
        }

        var changed = !_dresserItemIds.SetEquals(next);
        _dresserItemIds.Clear();
        _dresserItemIds.UnionWith(next);

        if (notify && changed) {
            Svc.Log.Debug($"[{nameof(DresserService)}] Dresser changed.");
            Changed?.Invoke();
        }
    }
}
