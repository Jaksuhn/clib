using Lumina.Excel.Sheets;

namespace clib.Extensions;

public static class ContentFinderConditionExtensions {
    extension(ContentFinderCondition row) {
        public string NameFormatted => Svc.SeStringEvaluator.EvaluateFromAddon(163, [row.Name]).ToString();
    }
}
