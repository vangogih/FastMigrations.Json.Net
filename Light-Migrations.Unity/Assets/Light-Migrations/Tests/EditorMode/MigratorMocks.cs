using System;
using System.Reflection;
using Light_Migrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Light_Migrations.Tests.EditorMode
{
    public sealed class ReadConverterMock : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var obj = JToken.Load(reader);
            return existingValue;
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType.GetCustomAttribute(typeof(MigratableAttribute)) == null;
        }
    }

    public sealed class LightMigrationsConverterMock : LigthMigrationsConverter
    {
        public int ReadJsonCalledCount;

        public int WriteJsonCalledCount;

        public LightMigrationsConverterMock(MigratorMissingMethodHandling methodHandling)
            : base(methodHandling) { }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            WriteJsonCalledCount++;
            base.WriteJson(writer, value, serializer);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            ReadJsonCalledCount++;
            return base.ReadJson(reader, objectType, existingValue, serializer);
        }
    }
}