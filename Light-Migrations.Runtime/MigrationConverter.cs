using System;
using Newtonsoft.Json;

namespace Light_Migrations.Runtime
{
    public class MigrationConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => throw new NotImplementedException();

        public override object ReadJson(JsonReader reader,
            Type objectType,
            object existingValue,
            JsonSerializer serializer) =>
            throw new NotImplementedException();

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}