using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace clib.Extensions;

public static class ReadOnlySePayloadExtensions {
    public static bool TryGetLinkType(this ReadOnlySePayloadSpan payload, out LinkMacroPayloadType type) {
        type = default;
        if (payload.Type != ReadOnlySePayloadType.Macro || payload.MacroCode != MacroCode.Link) return false;
        if (!payload.TryGetExpression(out var e) || !e.TryGetInt(out var value)) return false;
        type = (LinkMacroPayloadType)value;
        return true;
    }
}
