using System;
using System.Collections.Generic;
using Light_Migrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

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
            return true;
        }
    }

    public sealed class MigratorMock : Migrator
    {
        public int ReadJsonCalledCount;
        public int WriteJsonCalledCount;

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
            Assert.AreEqual(1, MethodCallHandler.VersionCalledCount[typeof(PersonV1)].MethodCallCount);
            Assert.AreEqual(1, migrator.ReadJsonCalledCount);
            Assert.AreEqual(0, migrator.WriteJsonCalledCount);
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
            Assert.AreEqual(1, MethodCallHandler.VersionCalledCount[typeof(PersonJsonCtor)].MethodCallCount);
            Assert.IsTrue(person.IsJsonCtorCalled);
        }

        [Test]
        public void ValidJsonV1_MoreThenOneConverted_Pass()
        {
            // given => json with field Version
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""Version"":1}";

            // when => we have more then one converter
            var migrator1 = new MigratorMock();
            var migrator2 = new ReadConverterMock();

            // then => there is no exception AND person is not an empty
            PersonV1 person = null;
            Assert.DoesNotThrow(() => { person = JsonConvert.DeserializeObject<PersonV1>(json, migrator1, migrator2); });
            Assert.NotNull(person);
        }

        [Test]
        public void ValidJsonWith2MigratableObject_Deserialize_MigratorCalled()
        {
            // given => json with 2 migratable objects inside
            var json = @"
{
""person1"":{""name"":""Alex Kozorezov"",""age"":27,""Version"":1},
""person2"":{""name"":""Mikhail Suvorov"",""age"":31,""Version"":1}
}";

            // when => deserialize json
            var migrator = new MigratorMock();
            var persons = JsonConvert.DeserializeObject<TwoPersonsNotMigratableMock>(json, migrator);

            // then => there is not exception
            Assert.NotNull(persons);
            // and => we call Migrate() on both objects
            Assert.AreEqual(2, MethodCallHandler.VersionCalledCount[typeof(PersonV1)].MethodCallCount);
            Assert.AreEqual(2, migrator.ReadJsonCalledCount);
        }

        [Test]
        public void MigratableObjectsInsideMigratableObject_Deserialize_MigratorCalled()
        {
            // given => json with migratable objects inside migratable object
            var json = @"
{
""person1"":{""name"":""Alex Kozorezov"",""age"":27,""Version"":1},
""person2"":{""name"":""Mikhail Suvorov"",""age"":31,""Version"":1}
}";
            // when => deserialize json
            var migrator = new MigratorMock();
            var persons = JsonConvert.DeserializeObject<TwoPersonsMigratableMock>(json, migrator);

            // then => there is not exception
            Assert.NotNull(persons);
            // and => we call Migrate() on both objects
            Assert.AreEqual(2, MethodCallHandler.VersionCalledCount[typeof(PersonV1)].MethodCallCount);
            // and => we call Migrate() on parent object
            Assert.AreEqual(1, MethodCallHandler.VersionCalledCount[typeof(TwoPersonsMigratableMock)].MethodCallCount);
            // and => we call ReadJson() on migrator 3 times
            Assert.AreEqual(3, migrator.ReadJsonCalledCount);
        }

        [Test]
        public void PersonInArray_Deserialize_MigratorCalled()
        {
            // given => jsom with array of migratable objects
            var json = @"
{
    ""persons"":
        [
            {""name"":""Alex Kozorezov"",""age"":27,""Version"":1},
            {""name"":""Mikhail Suvorov"",""age"":31,""Version"":1}
        ]
}";

            // when => deserialize json with migrator
            var migrator = new MigratorMock();
            var persons = JsonConvert.DeserializeObject<PersonsDataStructureMock<PersonV1[]>>(json, migrator);

            // then => there is no exception
            Assert.NotNull(persons);
            // and => we call Migrate() on each object
            Assert.AreEqual(2, MethodCallHandler.VersionCalledCount[typeof(PersonV1)].MethodCallCount);
        }

        [Test]
        public void PersonInList_Deserialize_MigratorCalled()
        {
            // given => jsom with list of migratable objects
            var json = @"
{
    ""persons"":
        [
            {""name"":""Alex Kozorezov"",""age"":27,""Version"":1},
            {""name"":""Mikhail Suvorov"",""age"":31,""Version"":1}
        ]
}";

            // when => deserialize json with migrator
            var migrator = new MigratorMock();
            var persons = JsonConvert.DeserializeObject<PersonsDataStructureMock<List<PersonV1>>>(json, migrator);

            // then => there is no exception
            Assert.NotNull(persons);
            // and => we call Migrate() on each object
            Assert.AreEqual(2, MethodCallHandler.VersionCalledCount[typeof(PersonV1)].MethodCallCount);
        }

        [Test]
        public void PersonInDictionary_Deserialize_MigratorCalled()
        {
            // given => jsom with dictionary of migratable objects
            var json = @"
{
    ""persons"":
    {
        ""person1"":{""name"":""Alex Kozorezov"",""age"":27,""Version"":1}, 
        ""person2"":{""name"":""Mikhail Suvorov"",""age"":31,""Version"":1}
    }
}";

            // when => deserialize json with migrator
            var migrator = new MigratorMock();
            var persons = JsonConvert.DeserializeObject<PersonsDataStructureMock<Dictionary<string, PersonV1>>>(json, migrator);

            // then => there is no exception
            Assert.NotNull(persons);
            // and => we call Migrate() on each object
            Assert.AreEqual(2, MethodCallHandler.VersionCalledCount[typeof(PersonV1)].MethodCallCount);
        }

        [Test]
        public void JsonWithVersion1_DeserializeAsVersion3_Success()
        {
            // given => json with field Version with value 1
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""Version"":1}";
            // when => deserialize json with migrator and type of version 3
            var migrator = new MigratorMock();
            var person = JsonConvert.DeserializeObject<PersonV2>(json, migrator);
            // then => there is no exception
            Assert.NotNull(person);
            // and => data inside object is correct
            Assert.AreEqual("Alex Kozorezov", person.FullName);
            Assert.AreEqual("Alex", person.Name);
            Assert.AreEqual("Kozorezov", person.Surname);
            Assert.AreEqual(1997, person.BirthYear);
        }

        [Test]
        public void JsonWithVersion1_DeserializeAsVersion3WithoutMigrator_Exception()
        {
            // given => json with field Version with value 1
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""Version"":1}";
            // when => try to deserialize json without migrator and type of version 3
            var person = JsonConvert.DeserializeObject<PersonV2>(json);
            // then => there is exception
            Assert.IsNotNull(person);
            Assert.AreEqual("Alex Kozorezov", person.Name);
            Assert.IsNull(person.Surname);
            Assert.IsNull(person.FullName);
            Assert.AreEqual(0, person.BirthYear);
        }

        // DONE: Separate files to assemblies
        // DONE: Case when we have JsonConstructor (it must be problematic)
        // DONE: Case when we have more then 1 converter (Add this as limitation)
        // DONE: Case when we have more then 1 IMigratable implementations inside one json
        // DONE: Case when we have to migrate from version 1 to 3
        // DONE: Rewrite Migrator implementation to allow to call migration sequentially to avoid writing boilerplate code on users side 
        // TODO: Add test case for populate and existing value (existingValue != null && serializer.ObjectCreationHandling != ObjectCreationHandling.Replace)
        // TODO: Ask Ilya Naumov to add recommendation to implement IMigtratable interface explicitly 
        [TearDown] public void TearDown() {MethodCallHandler.Clear(); }

        [SetUp] public void SetUp() { }
    }
}