using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Light_Migrations.Runtime
{
    public class Migrator : JsonConverter<IMigratable>
    {
        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, IMigratable value, JsonSerializer serializer) { }

        public override IMigratable ReadJson(JsonReader reader, Type objectType, IMigratable existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var versionToken = jObject["Version"];
            int versionValue;
            var instance = (IMigratable)Activator.CreateInstance(objectType);

            if (versionToken == null)
            {
                jObject.Add("Version", instance.Version);
                versionValue = instance.Version;
            }
            else
                versionValue = versionToken.ToObject<int>();

            instance.Migrate(ref jObject, versionValue, instance.Version);
            serializer.Populate(jObject.CreateReader(), instance);

            return instance;
        }
    }
}