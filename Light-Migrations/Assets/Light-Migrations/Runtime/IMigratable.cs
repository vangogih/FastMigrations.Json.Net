using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Light_Migrations.Runtime
{
    public interface IMigratable
    {
        [JsonProperty("Version", Required = Required.Always)]
        int Version { get; }

        void Migrate(ref JObject jsonObj, int from, int to);
    }
}