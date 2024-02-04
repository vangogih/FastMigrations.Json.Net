using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

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

    public sealed class MigratorMock : Migrator
    {
        public bool IsReadCalled;
        public bool IsWriteCalled;

        public override IMigratable ReadJson(JsonReader reader, Type objectType, IMigratable existingValue,
            bool hasExistingValue, JsonSerializer serializer)
        {
            IsReadCalled = true;

            return base.ReadJson(reader, objectType, existingValue, hasExistingValue,
                serializer);
        }

        public override void WriteJson(JsonWriter writer, IMigratable value, JsonSerializer serializer)
        {
            IsWriteCalled = true;
            base.WriteJson(writer, value, serializer);
        }
    }

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

    public sealed class ExampleBddTest
    {
        private JsonSerializerSettings _setting;

        [Test]
        public void ValidJsonV1_Deserialize_MigratorCalled()
        {
            // given => json with field Version
            var json = @"{""name"":""John Doe"",""age"":33,""Version"":1}";

            // when => deserialize json
            var migrator = new MigratorMock();
            var person = JsonConvert.DeserializeObject<PersonV1>(json, migrator);

            // then => we run migrator and call Migrate() on model
            Assert.NotNull(person);
            Assert.IsTrue(person.IsMigrationCalled);
            Assert.IsTrue(migrator.IsReadCalled);
            Assert.IsFalse(migrator.IsWriteCalled);
        }

        // TODO: Case when we have JsonConstructor
        // TODO: Case when we have more then 1 converter
        // TODO: Case when we have more then 1 IMigratable implementations inside one json
        // TODO: Case when we have to migrate from version 1 to 3

        [TearDown] public void TearDown() { }

        [SetUp] public void SetUp() { }
    }
}