using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility.Raii;
using Lumina.Excel.Sheets;
using System.Diagnostics.CodeAnalysis;

namespace clib.Extensions;

public static class ImGuiExtensions {
    private static string searchResultsQuery = string.Empty;
    private static double lastSearchTime;
    private static Item[] itemSearchResults = [];

    extension(ImGui) {
        public static bool AddItemPopupButton([NotNullWhen(true)] out Item? result, string? buttonLabel = null, Vector2? size = null, Func<Item, bool>? itemSheetFilter = null) {
            result = null;
            if (ImGui.Button(buttonLabel ?? "Add Item", size ?? new Vector2(-1, 0))) {
                searchResultsQuery = "";
                ImGui.OpenPopup("item_search_add");
            }

            using var popup = ImRaii.Popup("item_search_add");
            if (!popup) return false;

            ImGui.Text("Search:");
            var currentTime = ImGui.GetTime();
            if (ImGui.GetTime() > lastSearchTime + 0.1f) {
                lastSearchTime = currentTime;
                itemSearchResults = !string.IsNullOrEmpty(searchResultsQuery)
                    ? uint.TryParse(searchResultsQuery, out var searchId)
                        ? [.. Svc.Data.GetExcelSheet<Item>().Where(x => itemSheetFilter?.Invoke(x) ?? true).Where(x => x.RowId == searchId).Take(20)]
                        : [.. Svc.Data.GetExcelSheet<Item>().Where(x => itemSheetFilter?.Invoke(x) ?? true).Where(x => x.Name.ToString().Contains(searchResultsQuery, StringComparison.OrdinalIgnoreCase)).Take(20)]
                    : [];
            }

            var maxWidth = Math.Max(300f, itemSearchResults.Select(item => ImGui.CalcTextSize($"[#{item.RowId}] {item.Name}").X).DefaultIfEmpty(200f).Max() + 30);
            ImGui.SetNextItemWidth(maxWidth);
            if (!ImGui.IsAnyItemActive()) // required for preventing stealing focus from selectables
                ImGui.SetKeyboardFocusHere();
            ImGui.InputText("##itemSearch", ref searchResultsQuery, 100);
            if (!string.IsNullOrEmpty(searchResultsQuery)) {
                using var child = ImRaii.Child("itemSearchResultsChild", new Vector2(maxWidth + 20, 220), true);
                if (!child) return false;

                foreach (var item in itemSearchResults) {
                    if (Svc.Texture.GetFromGameIcon(new GameIconLookup { IconId = item.Icon }).GetWrapOrDefault() is { Handle: var handle }) {
                        ImGui.Image(handle, new Vector2(16, 16));
                        ImGui.SameLine();
                    }

                    if (!ImGui.Selectable($" [#{item.RowId}] {item.Name}")) continue;
                    result = item;
                    ImGui.CloseCurrentPopup();
                    return true;
                }
            }
            return false;
        }

        public static bool AddCustomItemPopupButton([NotNullWhen(true)] out Item? result, IReadOnlyList<(uint ItemId, string DisplayName)> customItems, string? buttonLabel = null, Vector2? size = null) {
            result = null;
            if (ImGui.Button(buttonLabel ?? "Add Item", size ?? new Vector2(-1, 0))) {
                ImGui.OpenPopup("custom_item_search_add");
            }

            using var popup = ImRaii.Popup("custom_item_search_add");
            if (!popup) return false;

            var maxWidth = Math.Max(300f, customItems.Select(item => ImGui.CalcTextSize($"[#{item.ItemId}] {item.DisplayName}").X).DefaultIfEmpty(200f).Max() + 30);
            using var child = ImRaii.Child("customItemSearchResultsChild", new Vector2(maxWidth + 20, 220), true);
            if (!child) return false;

            foreach (var (itemId, displayName) in customItems) {
                if (Svc.Data.TryGetRow<Item>(itemId, out var item)) {
                    if (Svc.Texture.GetFromGameIcon(new GameIconLookup { IconId = item.Icon }).GetWrapOrDefault() is { Handle: var handle }) {
                        ImGui.Image(handle, new Vector2(16, 16));
                        ImGui.SameLine();
                    }

                    if (!ImGui.Selectable($" [#{itemId}] {displayName}")) continue;
                    result = item;
                    ImGui.CloseCurrentPopup();
                    return true;
                }
            }
            return false;
        }

        public static void TooltipOnHover(string text) {
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip(text);
        }

        public static void TooltipOnHover(bool condition, string text) {
            if (condition && ImGui.IsItemHovered())
                ImGui.SetTooltip(text);
        }

        public static bool IsItemClickedWithModifier(ImGuiMouseButton button, ImGuiModFlags modifier) => ImGui.IsItemClicked(button) && ImGui.GetIO().KeyMods.HasFlag(modifier);

        public static bool IsItemClickedNoModifiers(ImGuiMouseButton button) => ImGui.IsItemClicked(button) && ImGui.GetIO().KeyMods == ImGuiModFlags.None;
    }
}
