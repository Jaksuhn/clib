using Dalamud.Configuration;
using Newtonsoft.Json.Linq;

namespace clib.Configuration;

/// <remarks>For pre-deserializing config migration, when the old JSON doesn't bind to <typeparamref name="T"/></remarks> 
public interface IConfigJsonMigration<T> where T : IPluginConfiguration {
    int TargetVersion { get; }

    /// <summary>
    /// Mutate <paramref name="root"/> in place. Do not set <c>Version</c>. <see cref="ConfigHelper"/> assigns <see cref="TargetVersion"/> after this returns
    /// </summary>
    void Migrate(JObject root);
}
