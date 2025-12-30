namespace clib.Extensions;

public static class IEnumerableExtensions {
    public static void ForEach<T>(this IEnumerable<T> _items, Action<T> action) {
        ArgumentNullException.ThrowIfNull(_items);
        ArgumentNullException.ThrowIfNull(action);

        foreach (var item in _items) {
            action(item);
        }
    }
}
