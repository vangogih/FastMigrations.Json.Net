using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Light_Migrations.Tests.EditorMode
{
    public interface IMigratable
    {
        [JsonProperty("Version", Required = Required.Always)]
        int Version { get; }

        void Migrate(ref JObject jsonObj, int from, int to);
    }

    public sealed class PersonV1 : IMigratable
    {
        public int Version => 1;

        [NonSerialized] public bool IsMigrationCalled;

        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;

        public void Migrate(ref JObject jsonObj, int from, int to)
        {
            IsMigrationCalled = true;
        }
    }

    public sealed class Migrator : JsonConverter<IMigratable>
    {
        public bool IsReadCalled;
        public bool IsWriteCalled;

        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, IMigratable value, JsonSerializer serializer)
        {
            IsWriteCalled = true;
        }

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

            IsReadCalled = true;
            return instance;
        }
    }

    public sealed class ExampleBddTest
    {
        private JsonSerializerSettings _setting;
        private string _v1;
        private string _v2;
        private string _v3;

        [Test]
        public void Migration_Test()
        {
            // given => json with field Version
            var json = @"{""name"":""John Doe"",""age"":33,""Version"":1}";

            // when => deserialize json
            var migrator = new Migrator();
            var person = JsonConvert.DeserializeObject<PersonV1>(json, migrator);

            // then => we run migrator and call Migrate() on model
            Assert.NotNull(person);
            Assert.IsTrue(person.IsMigrationCalled);
            Assert.IsTrue(migrator.IsReadCalled);
            Assert.IsFalse(migrator.IsWriteCalled);
        }

        [TearDown] public void TearDown() { }

        [SetUp] public void SetUp() { }
    }
}