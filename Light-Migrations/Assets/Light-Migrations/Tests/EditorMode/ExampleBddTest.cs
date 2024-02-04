using System;
using Light_Migrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Light_Migrations.Tests.EditorMode
{
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
    
    public sealed class PersonJsonCtor : IMigratable
    {
        public int Version => 1;

        [NonSerialized] public bool IsMigrationCalled;
        [NonSerialized] public bool IsJsonCtorCalled;

        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;

        public PersonJsonCtor()
        {
        }
        
        [JsonConstructor]
        public PersonJsonCtor(string name, int age)
        {
            Name = name;
            Age = age;
            IsJsonCtorCalled = true;
        }

        public void Migrate(ref JObject jsonObj, int from, int to)
        {
            IsMigrationCalled = true;
        }
    }
    
    public sealed class ReadConverterMock : JsonConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var obj = JObject.Load(reader);
            return existingValue;
        }

        public override bool CanConvert(Type objectType)
        {
            return true;
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

    public sealed class ExampleBddTest
    {
        private JsonSerializerSettings _setting;

        [Test]
        public void ValidJsonV1_Deserialize_MigratorCalled()
        {
            // given => json with field Version
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""Version"":1}";

            // when => deserialize json
            var migrator = new MigratorMock();
            var person = JsonConvert.DeserializeObject<PersonV1>(json, migrator);

            // then => we run migrator and call Migrate() on model
            Assert.NotNull(person);
            Assert.IsTrue(person.IsMigrationCalled);
            Assert.IsTrue(migrator.IsReadCalled);
            Assert.IsFalse(migrator.IsWriteCalled);
        }

        [Test]
        public void ValidJsonV1_DeserializePersonWithJsonCtor_CtorCalledMigratorCalled()
        {
            // given => json with field Version
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""Version"":1}";
            
            // when => deserialize json
            var migrator = new MigratorMock();
            var person = JsonConvert.DeserializeObject<PersonJsonCtor>(json, migrator);
            
            // then => we run migrator and call Migrate() and JsonCtor on model
            Assert.NotNull(person);
            Assert.IsTrue(person.IsMigrationCalled);
            Assert.IsTrue(person.IsJsonCtorCalled);
        }

        [Test]
        [Ignore("This is limitation of the current implementation")]
        public void ValidJsonV1_MoreThenOneConverted_Pass()
        {
            // given => json with field Version
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""Version"":1}";
            
            // when => we have more then one converter
            var migrator1 = new MigratorMock();
            var migrator2 = new ReadConverterMock();
            
            // then => there is no exception AND person is not an empty
            PersonV1 person = null;
            Assert.DoesNotThrow(() =>
            {
                person = JsonConvert.DeserializeObject<PersonV1>(json, migrator1, migrator2);
            });
            Assert.NotNull(person);
        }

        // DONE: Separate files to assemblies
        // DONE: Case when we have JsonConstructor (it must be problematic)
        // DONE: Case when we have more then 1 converter (Add this as limitation)
        // TODO: Case when we have more then 1 IMigratable implementations inside one json
        // TODO: Case when we have to migrate from version 1 to 3

        [TearDown] public void TearDown() { }

        [SetUp] public void SetUp() { }
    }
}