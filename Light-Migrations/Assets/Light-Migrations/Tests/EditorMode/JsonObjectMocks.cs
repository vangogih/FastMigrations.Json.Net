using System;
using Light_Migrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Light_Migrations.Tests.EditorMode
{
    public sealed class PersonV1 : IMigratable
    {
        public int Version => 1;

        [NonSerialized] public int MigrationCalledCount;

        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;

        public void Migrate(ref JObject jsonObj, int from, int to)
        {
            MigrationCalledCount++;
        }
    }
    
    public sealed class PersonV3 : IMigratable
    {
        public int Version => 3;

        [JsonProperty("fullName")] public string FullName;
        [JsonProperty("name")] public string Name;
        [JsonProperty("surname")] public string Surname;
        [JsonProperty("birthYear")] public int BirthYear;

        public void Migrate(ref JObject jsonObj, int from, int to)
        {
            if (from == 1 && to == 3)
                From1To3(jsonObj);

            if (from == 3 && to == 10)
            {
                //From3To4(jsonObj);
                //From4To5(jsonObj);
                //From6To7(jsonObj);
                //From7To8(jsonObj);
                //From8To9(jsonObj);
                //From9To10(jsonObj);
            }

            if (from == 4 && to == 10)
            {
                //From4To5(jsonObj);
                //From6To7(jsonObj);
                //From7To8(jsonObj);
                //From8To9(jsonObj);
                //From9To10(jsonObj);
            }
            
            if (from == 5 && to == 10)
            {
                //From6To7(jsonObj);
                //From7To8(jsonObj);
                //From8To9(jsonObj);
                //From9To10(jsonObj);
            }
            
            if (from == 6 && to == 10)
            {
                //From7To8(jsonObj);
                //From8To9(jsonObj);
                //From9To10(jsonObj);
            }
            
            if (from == 7 && to == 10)
            {
                //From8To9(jsonObj);
                //From9To10(jsonObj);
            }
            
            if (from == 8 && to == 10)
            {
                //From9To10(jsonObj);
            }
            
            if (from == 9 && to == 10)
            {
                //From9To10(jsonObj);
            }
            
            if (from == 10 && to == 10)
            {
                //From9To10(jsonObj);
            }
        }

        private static void From1To3(JObject jsonObj)
        {
            JToken nameToken = jsonObj["name"];
            jsonObj.Add("fullName", nameToken);

            JToken ageToken = jsonObj["age"];
            jsonObj.Add("birthYear", DateTime.Today.Year - ageToken.ToObject<int>());
            jsonObj.Remove("age");

            var oldNameSplit = nameToken.ToObject<string>()!.Split(' ');
            jsonObj.Remove("name");
            jsonObj.Add("name", oldNameSplit[0]);
            jsonObj.Add("surname", oldNameSplit[1]);
        }
    }

    public sealed class PersonJsonCtor : IMigratable
    {
        public int Version => 1;

        [NonSerialized] public bool IsMigrationCalled;
        [NonSerialized] public bool IsJsonCtorCalled;

        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;

        public PersonJsonCtor() { }

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

    public class TwoPersonsNotMigratableMock
    {
        [JsonProperty("person1")] public PersonV1 Person1;
        [JsonProperty("person2")] public PersonV1 Person2;
    }
    
    public class TwoPersonsMigratableMock : IMigratable
    {
        public int Version => 1;
        [NonSerialized] public int MigrationCalledCount;

        [JsonProperty("person1")] public PersonV1 Person1;
        [JsonProperty("person2")] public PersonV1 Person2;

        public void Migrate(ref JObject jsonObj, int from, int to)
        {
            MigrationCalledCount++;
        }
    }

    public class PersonsDataStructureMock<T>
    {
        [JsonProperty("persons")] public T Persons;
    }
}