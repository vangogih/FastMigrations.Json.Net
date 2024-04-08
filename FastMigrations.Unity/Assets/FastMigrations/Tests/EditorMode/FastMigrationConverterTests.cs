using System.Collections.Generic;
using FastMigrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using NUnit.Framework;

namespace FastMigrations.Tests.EditorMode
{
    public sealed class FastMigrationConverterTests
    {
        [Test]
        public void EmptyJson_Deserialize_DoesNotThrow()
        {
            var json = @"{}";
            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);

            PersonV0WithoutMigrateMethod person = null;
            Assert.DoesNotThrow(() => person = JsonConvert.DeserializeObject<PersonV0WithoutMigrateMethod>(json, migrator));
            Assert.IsNotNull(person);
        }

        [Test]
        public void JsonWithoutVersion_DeserializeV0_DoesNotThrow()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27 }";
            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);

            Assert.DoesNotThrow(() => JsonConvert.DeserializeObject<PersonV0WithoutMigrateMethod>(json, migrator));
        }

        [Test]
        public void JsonV0_DeserializeV0_Pass()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27, ""JsonVersion"":0 }";
            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            Assert.DoesNotThrow(() => JsonConvert.DeserializeObject<PersonV0WithoutMigrateMethod>(json, migrator));
        }

        [Test]
        public void JsonV1_DeserializeStruct_Pass()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);

            PersonStructV1 person = default;
            Assert.DoesNotThrow(() => person = JsonConvert.DeserializeObject<PersonStructV1>(json, migrator));

            Assert.AreEqual("Alex Kozorezov", person.Name);
            Assert.AreEqual(27, person.Age);
        }

        [Test]
        public void JsonV1_DeserializeV1_NotCallMigrate()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27, ""JsonVersion"":1}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var person = JsonConvert.DeserializeObject<PersonV1>(json, migrator);

            Assert.NotNull(person);
            Assert.IsFalse(MethodCallHandler.MethodsCallInfoByType.ContainsKey(typeof(PersonV1)));
            Assert.AreEqual(1, migrator.ReadJsonCalledCount);
            Assert.AreEqual(0, migrator.WriteJsonCalledCount);
        }

        [Test]
        public void JsonWithoutVersion_DeserializeV1_MigratorCalled()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27 }";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var person = JsonConvert.DeserializeObject<PersonV1>(json, migrator);

            Assert.NotNull(person);
            Assert.AreEqual(1, MethodCallHandler.MethodsCallInfoByType[typeof(PersonV1)].MethodCallCount);
            Assert.AreEqual(1, migrator.ReadJsonCalledCount);
            Assert.AreEqual(0, migrator.WriteJsonCalledCount);
        }

        [Test]
        public void JsonWithoutVersion_DeserializePersonWithJsonCtor_CtorCalledMigratorCalled()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var person = JsonConvert.DeserializeObject<PersonJsonCtor>(json, migrator);

            Assert.NotNull(person);
            Assert.AreEqual(1, MethodCallHandler.MethodsCallInfoByType[typeof(PersonJsonCtor)].MethodCallCount);
            Assert.IsTrue(person.IsJsonCtorCalled);
        }

        [Test]
        public void JsonWithoutVersion_DeserializeWithOtherMigrator_Pass()
        {
            var json = @"{
""person1"":{""name"":""Alex Kozorezov"",""age"":27},
""Version"":""1.2.3""
}";

            var migrator1 = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var migrator2 = new VersionConverter();

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
        public void JsonWithoutVersion_DeserializeV1WithoutMigrationMethod_ThrowMigrationException()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27 }";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);

            Assert.Throws<MigrationException>(() => JsonConvert.DeserializeObject<PersonV1WithoutMigrationMethod>(json, migrator));
        }

        [Test]
        public void JsonWithoutVersion_DeserializeAsV1WithIgnore_Pass()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27 }";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.Ignore);

            var person = JsonConvert.DeserializeObject<PersonV1WithoutMigrationMethod>(json, migrator);
            Assert.NotNull(person);
        }

        [Test]
        public void JsonWith2MigratableObject_Deserialize_MigratorCalled()
        {
            var json = @"
{
    ""person1"":{""name"":""Alex Kozorezov"",""age"":27},
    ""person2"":{""name"":""Mikhail Suvorov"",""age"":31}
}";
            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var persons = JsonConvert.DeserializeObject<TwoPersonsV1NotMigratableMock>(json, migrator);

            Assert.NotNull(persons);
            Assert.AreEqual(2, MethodCallHandler.MethodsCallInfoByType[typeof(PersonV1)].MethodCallCount);
            Assert.AreEqual(2, migrator.ReadJsonCalledCount);
        }

        [Test]
        public void MigratableObjectsInsideMigratableObject_Deserialize_MigratorCalled()
        {
            var json = @"
{
    ""person1"":{""name"":""Alex Kozorezov"",""age"":27},
    ""person2"":{""name"":""Mikhail Suvorov"",""age"":31}
}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var persons = JsonConvert.DeserializeObject<TwoPersonsMigratableMock>(json, migrator);

            Assert.NotNull(persons);
            Assert.AreEqual(2, MethodCallHandler.MethodsCallInfoByType[typeof(PersonV1)].MethodCallCount);
            Assert.AreEqual(1, MethodCallHandler.MethodsCallInfoByType[typeof(TwoPersonsMigratableMock)].MethodCallCount);
            Assert.AreEqual(3, migrator.ReadJsonCalledCount);
        }

        [Test]
        public void MigratableParentWithMigratableChild_Deserialize_MigratorCalled()
        {
            var json = @"{}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.Ignore);
            var childV10 = JsonConvert.DeserializeObject<ChildV10Mock>(json, migrator);

            Assert.NotNull(childV10);
            MethodCallHandler.MethodsCallInfo childCallInfo = MethodCallHandler.MethodsCallInfoByType[typeof(ChildV10Mock)];
            MethodCallHandler.MethodsCallInfo parentCallInfo = MethodCallHandler.MethodsCallInfoByType[typeof(ParentMock)];

            Assert.AreEqual(3, childCallInfo.MethodCallCount);
            Assert.AreEqual(1, childCallInfo.VersionsCalled[0]);
            Assert.AreEqual(2, childCallInfo.VersionsCalled[1]);
            Assert.AreEqual(10, childCallInfo.VersionsCalled[2]);

            Assert.AreEqual(2, parentCallInfo.MethodCallCount);
            Assert.AreEqual(1, parentCallInfo.VersionsCalled[0]);
            Assert.AreEqual(2, parentCallInfo.VersionsCalled[1]);
        }

        [Test]
        public void NestedJson_Deserialize_Pass()
        {
            var json = @"
{""name"":""Alex Kozorezov"",
    ""person"":{""name"":""Mikhail Suvorov"", 
        ""person"": {""name"":""I'm fan of CySharp fan""}
    }
}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var person = JsonConvert.DeserializeObject<PersonV1NestedMock>(json, migrator);

            Assert.NotNull(person);
            Assert.AreEqual("Alex Kozorezov", person.Name);

            MethodCallHandler.MethodsCallInfo methodsCallInfo = MethodCallHandler.MethodsCallInfoByType[typeof(PersonV1NestedMock)];
            Assert.AreEqual(1, methodsCallInfo.MethodCallCount);
            Assert.AreEqual(1, methodsCallInfo.VersionsCalled[0]);
        }

        [Test]
        public void JsonWithRefs_DeserializeWith_PreserveReferencesHandlingAll_Pass()
        {
            // from: https://www.newtonsoft.com/json/help/html/preservereferenceshandlingobject.htm
            var json = @"
{
  ""$id"": ""1"",
  ""Name"": ""My Documents"",
  ""Parent"": {
    ""$id"": ""2"",
    ""Name"": ""Root"",
    ""Parent"": null,
    ""Files"": 
    [
        {
            ""$ref"": ""3""
        }
    ]
  },
  ""Files"": {
    ""$id"": ""3"",
    ""$values"": [
      {
        ""$id"": ""4"",
        ""Name"": ""ImportantLegalDocument.docx"",
        ""Parent"": {
          ""$ref"": ""1""
        }
      },
      {
          ""$ref"": ""4""
      }
    ]
  }
}
";
            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            Directory directory = JsonConvert.DeserializeObject<Directory>(json, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.All, Converters = new List<JsonConverter> { migrator } });

            Assert.NotNull(directory);

            var directoryCallInfo = MethodCallHandler.MethodsCallInfoByType[typeof(Directory)];
            var fileCallInfo = MethodCallHandler.MethodsCallInfoByType[typeof(File)];

            Assert.AreEqual(1, directoryCallInfo.MethodCallCount);
            Assert.AreEqual(1, fileCallInfo.MethodCallCount);
        }

        [Test]
        public void JsonWithRefs_DeserializeWith_PreserveReferencesHandlingObjects_Pass()
        {
            // from: https://www.newtonsoft.com/json/help/html/preservereferenceshandlingobject.htm
            var json = @"
{
  ""$id"": ""1"",
   ""Name"": ""My Documents"",
   ""Parent"": {
     ""$id"": ""2"",
     ""Name"": ""Root"",
     ""Parent"": null,
     ""Files"":
    [
        {
         ""$ref"": ""3""
        }
    ]
   },
   ""Files"": [
     {
       ""$id"": ""3"",
       ""Name"": ""ImportantLegalDocument.docx"",
       ""Parent"": {
         ""$ref"": ""1""
       }
     },
    {
        ""$ref"": ""3""
    }
   ]
 }
";
            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            Directory directory = JsonConvert.DeserializeObject<Directory>(json, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects, Converters = new List<JsonConverter> { migrator } });

            Assert.NotNull(directory);

            var directoryCallInfo = MethodCallHandler.MethodsCallInfoByType[typeof(Directory)];
            var fileCallInfo = MethodCallHandler.MethodsCallInfoByType[typeof(File)];

            Assert.AreEqual(1, directoryCallInfo.MethodCallCount);
            Assert.AreEqual(1, fileCallInfo.MethodCallCount);
        }

        [Test]
        public void Array_Deserialize_MigratorCalled()
        {
            var json = @"
{
    ""persons"":
        [
            {""name"":""Alex Kozorezov"",""age"":27},
            {""name"":""Mikhail Suvorov"",""age"":31}
        ]
}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var persons = JsonConvert.DeserializeObject<PersonsDataStructureMock<PersonV1[]>>(json, migrator);

            Assert.NotNull(persons);
            Assert.AreEqual(2, MethodCallHandler.MethodsCallInfoByType[typeof(PersonV1)].MethodCallCount);
        }

        [Test]
        public void List_Deserialize_MigratorCalled()
        {
            var json = @"
{
    ""persons"":
        [
            {""name"":""Alex Kozorezov"",""age"":27},
            {""name"":""Mikhail Suvorov"",""age"":31}
        ]
}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var persons = JsonConvert.DeserializeObject<PersonsDataStructureMock<List<PersonV1>>>(json, migrator);

            Assert.NotNull(persons);
            Assert.AreEqual(2, MethodCallHandler.MethodsCallInfoByType[typeof(PersonV1)].MethodCallCount);
        }

        [Test]
        public void Dictionary_Deserialize_MigratorCalled()
        {
            var json = @"
{
    ""persons"":
    {
        ""person1"":{""name"":""Alex Kozorezov"",""age"":27}, 
        ""person2"":{""name"":""Mikhail Suvorov"",""age"":31}
    }
}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var persons = JsonConvert.DeserializeObject<PersonsDataStructureMock<Dictionary<string, PersonV1>>>(json, migrator);

            Assert.NotNull(persons);
            Assert.AreEqual(2, MethodCallHandler.MethodsCallInfoByType[typeof(PersonV1)].MethodCallCount);
        }

        [Test]
        public void JsonWithoutVersion_DeserializeAsVersion2_Pass()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var person = JsonConvert.DeserializeObject<PersonV2>(json, migrator);

            Assert.NotNull(person);
            Assert.AreEqual("Alex Kozorezov", person.FullName);
            Assert.AreEqual("Alex", person.Name);
            Assert.AreEqual("Kozorezov", person.Surname);
            Assert.AreEqual(1997, person.BirthYear);

            MethodCallHandler.MethodsCallInfo callInfo = MethodCallHandler.MethodsCallInfoByType[typeof(PersonV2)];
            Assert.AreEqual(2, callInfo.MethodCallCount);
            Assert.AreEqual(1, callInfo.VersionsCalled[0]);
            Assert.AreEqual(2, callInfo.VersionsCalled[1]);
        }

        [Test]
        public void JsonV1_DeserializeAsVersion2_Pass()
        {
            var json = @"{""name"":""Alex Kozorezov"",""age"":27, ""JsonVersion"":1 }";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var person = JsonConvert.DeserializeObject<PersonV2>(json, migrator);

            Assert.NotNull(person);
            Assert.AreEqual("Alex Kozorezov", person.FullName);
            Assert.AreEqual("Alex", person.Name);
            Assert.AreEqual("Kozorezov", person.Surname);
            Assert.AreEqual(1997, person.BirthYear);

            MethodCallHandler.MethodsCallInfo callInfo = MethodCallHandler.MethodsCallInfoByType[typeof(PersonV2)];
            Assert.AreEqual(1, callInfo.MethodCallCount);
            Assert.AreEqual(2, callInfo.VersionsCalled[0]);
        }

        [Test]
        public void TwoPersonsWithoutVersion_Populate_MigrationsCalled()
        {
            var json = @"
{
    ""person1"":{""name"":""Alex Kozorezov"",""age"":27 },
    ""person2"":{""name"":""Mikhail Suvorov"",""age"":31 }
}";

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var person = new TwoPersonsV1NotMigratableMock { Person1 = new PersonV1() };
            JsonConvert.PopulateObject(json, person, new JsonSerializerSettings { Converters = { migrator } });

            MethodCallHandler.MethodsCallInfo callInfo = MethodCallHandler.MethodsCallInfoByType[typeof(PersonV1)];
            Assert.AreEqual(2, callInfo.MethodCallCount);
            Assert.AreEqual(1, callInfo.VersionsCalled[0]);
            Assert.AreEqual(1, callInfo.VersionsCalled[1]);
        }

        [Test]
        public void WriteJson()
        {
            var persomV0 = new PersonV0WithoutMigrateMethod { Name = "Alex Kozorezov", Age = 27 };
            var personV1 = new PersonV1 { Name = "Alex Kozorezov", Age = 27 };
            var personV2 = new PersonV2 { FullName = "Alex Kozorezov", Name = "Alex", Surname = "Kozorezov", BirthYear = 1997 };

            var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
            var jsonV0 = JsonConvert.SerializeObject(persomV0, migrator);
            var jsonV1 = JsonConvert.SerializeObject(personV1, migrator);
            var jsonV2 = JsonConvert.SerializeObject(personV2, migrator);

            Assert.IsFalse(jsonV0.Contains($@"""{MigratorConstants.VersionJsonFieldName}"":0"));
            Assert.IsTrue(jsonV1.Contains($@"""{MigratorConstants.VersionJsonFieldName}"":1"));
            Assert.IsTrue(jsonV2.Contains($@"""{MigratorConstants.VersionJsonFieldName}"":2"));
        }

        [TearDown]
        public void TearDown()
        {
            MethodCallHandler.Clear();
        }

        [SetUp] public void SetUp() { }
    }
}