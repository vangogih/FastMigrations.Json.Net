using System;
using System.Reflection;
using FastMigrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FastMigrations.Tests.EditorMode
{
    public sealed class FastMigrationsConverterMock : FastMigrationsConverter
    {
        public int ReadJsonCalledCount;

        public int WriteJsonCalledCount;

        public FastMigrationsConverterMock(MigratorMissingMethodHandling methodHandling)
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