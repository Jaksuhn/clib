namespace clib.Extensions;

public static class ArrayExtensions {
    extension<T>(T[] items) {
        public void ForEach(Action<T> action) {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(action);

            for (var i = 0; i < items.Length; i++) {
                action(items[i]);
            }
        }
    }

    extension<T1, T2>((T1, T2)[] items) {
        public void ForEach(Action<T1, T2> action) {
            ArgumentNullException.ThrowIfNull(items);
            ArgumentNullException.ThrowIfNull(action);

            for (var i = 0; i < items.Length; i++) {
                action(items[i].Item1, items[i].Item2);
            }
        }
    }
}
