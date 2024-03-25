using System.Collections.Generic;
using Light_Migrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;

namespace Light_Migrations.Tests.EditorMode
{
    public sealed class LightMigrationConverterTests
    {
        [Test]
        public void EmptyJson_Deserialize_Exception()
        {
            // given => empty json
            var json = @"{}";

            // when => deserialize json
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);

            // then => throws MigrationException
            PersonV1 person = null;
            Assert.DoesNotThrow(() => person = JsonConvert.DeserializeObject<PersonV1>(json, migrator));
            Assert.IsNotNull(person);
        }

        [Test]
        public void ValidJsonV1_DeserializeStruct_Pass()
        {
            // given => json with field Version
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}";

            // when => deserialize json as struct
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);

            // then => there is no exception
            PersonStructV1 person = default;
            Assert.DoesNotThrow(() => person = JsonConvert.DeserializeObject<PersonStructV1>(json, migrator));

            // and => person is not an empty
            Assert.AreEqual("Alex Kozorezov", person.Name);
            Assert.AreEqual(27, person.Age);
        }

        [Test]
        public void ValidJsonV1_Deserialize_MigratorCalled()
        {
            // given => json with field Version
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}";

            // when => deserialize json
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
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
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}";

            // when => deserialize json
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
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
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}";

            // when => we have more then one converter
            var migrator1 = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var randomConverter = new ReadConverterMock();

            // then => there is no exception AND person is not an empty
            PersonV1 person = null;
            Assert.DoesNotThrow(() => { person = JsonConvert.DeserializeObject<PersonV1>(json, migrator1, randomConverter); });
            Assert.NotNull(person);
        }

        [Test]
        public void ValidJsonV1_DeserealizeWithOtherMigrator_Pass()
        {
            // given => json with field Version and version
            var json = @"{
""person1"":{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1},
""Version"":""1.2.3""
}";

            // when => we have more then one migrator
            var migrator1 = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var migrator2 = new VersionConverter();

            // then => there is no exception
            TwoPersonsTwoDifferentMigratableMock persons = null;
            Assert.DoesNotThrow(() => persons = JsonConvert.DeserializeObject<TwoPersonsTwoDifferentMigratableMock>(json, migrator1, migrator2));

            Assert.NotNull(persons);

            Assert.NotNull(persons.Person1);
            Assert.AreEqual("Alex Kozorezov", persons.Person1.Name);
            Assert.AreEqual(27, persons.Person1.Age);

            Assert.NotNull(persons.Version);
            Assert.AreEqual(persons.Version.Major, 1);
            Assert.AreEqual(persons.Version.Minor, 2);
            Assert.AreEqual(persons.Version.Build, 3);
        }

        [Test]
        public void JsonWithNullValue_Deserialize_Exception()
        {
            // given => json with null value
            var json = @"
{
    ""person1"":{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1},
    ""person2"":null
}";

            // when => deserialize json
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);

            // then => there is no exception
            Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<TwoPersonsMigratableMock>(json, migrator));
            Assert.Throws<JsonReaderException>(() => JsonConvert.DeserializeObject<TwoPersonsNotMigratableMock>(json, migrator));
        }

        [Test]
        public void ValidJsonV1_DeserializeAsV2WithoutMigrationMethod_ThrowMigrationException()
        {
            // given => json with field Version
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}";

            // when => deserializes json without migration method
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);

            // then => throws MigrationException
            Assert.Throws<MigrationException>(() => JsonConvert.DeserializeObject<PersonWithoutMigrationMethod>(json, migrator));
        }

        [Test]
        public void ValidJsonV1_DeserializeAsV2WithIgnoreMigrationMethod_Pass()
        {
            // given => json with field Version
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}";

            // when => deserializes json with ignore migration method
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.Ignore);

            // then => there is no exception
            var person = JsonConvert.DeserializeObject<PersonWithoutMigrationMethod>(json, migrator);
            Assert.NotNull(person);
        }

        [Test]
        public void ValidJsonWith2MigratableObject_Deserialize_MigratorCalled()
        {
            // given => json with 2 migratable objects inside
            var json = @"
{
    ""person1"":{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1},
    ""person2"":{""name"":""Mikhail Suvorov"",""age"":31,""MigrationVersion"":1}
}";

            // when => deserialize json
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
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
    ""person1"":{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1},
    ""person2"":{""name"":""Mikhail Suvorov"",""age"":31,""MigrationVersion"":1}
}";
            // when => deserialize json
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
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
            {""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1},
            {""name"":""Mikhail Suvorov"",""age"":31,""MigrationVersion"":1}
        ]
}";

            // when => deserialize json with migrator
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
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
            {""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1},
            {""name"":""Mikhail Suvorov"",""age"":31,""MigrationVersion"":1}
        ]
}";

            // when => deserialize json with migrator
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
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
        ""person1"":{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}, 
        ""person2"":{""name"":""Mikhail Suvorov"",""age"":31,""MigrationVersion"":1}
    }
}";

            // when => deserialize json with migrator
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var persons = JsonConvert.DeserializeObject<PersonsDataStructureMock<Dictionary<string, PersonV1>>>(json, migrator);

            // then => there is no exception
            Assert.NotNull(persons);
            // and => we call Migrate() on each object
            Assert.AreEqual(2, MethodCallHandler.VersionCalledCount[typeof(PersonV1)].MethodCallCount);
        }

        [Test]
        public void JsonWithVersion1_DeserializeAsVersion3_Pass()
        {
            // given => json with field Version with value 1
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}";

            // when => deserialize json with migrator and type of version 3
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
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
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}";

            // when => try to deserialize json without migrator and type of version 3
            var person = JsonConvert.DeserializeObject<PersonV2>(json);

            // then => there is exception
            Assert.IsNotNull(person);
            Assert.AreEqual("Alex Kozorezov", person.Name);
            Assert.IsNull(person.Surname);
            Assert.IsNull(person.FullName);
            Assert.AreEqual(0, person.BirthYear);
        }

        [Test]
        public void ValidJsonV1_PopulateWithMigrator_Pass()
        {
            // given => json with field Version
            var json = @"{""name"":""Alex Kozorezov"",""age"":27,""MigrationVersion"":1}";

            // when => populate json with migrator
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var person = new PersonV1 { Name = "Mikhail Suvorov", Age = 31 };
            JsonConvert.PopulateObject(json, person, new JsonSerializerSettings { Converters = { migrator } });

            // then => old value in object is not replaced
            Assert.AreEqual("Alex Kozorezov", person.Name);
            Assert.AreEqual(27, person.Age);
        }

        [Test]
        public void WriteJson()
        {
            // given => object to serialize
            var personV1 = new PersonV1 { Name = "Alex Kozorezov", Age = 27 };
            var personV2 = new PersonV2 { FullName = "Alex Kozorezov", Name = "Alex", Surname = "Kozorezov", BirthYear = 1997 };

            // when => serialize object
            var migrator = new LightMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var jsonV1 = JsonConvert.SerializeObject(personV1, migrator);
            var jsonV2 = JsonConvert.SerializeObject(personV2, migrator);

            // then => there is no exception and version is written in string
            Assert.IsTrue(jsonV1.Contains(@"""MigrationVersion"":1"));
            Assert.IsTrue(jsonV2.Contains(@"""MigrationVersion"":2"));
        }

        [TearDown]
        public void TearDown()
        {
            MethodCallHandler.Clear();
        }

        [SetUp] public void SetUp() { }
    }
}