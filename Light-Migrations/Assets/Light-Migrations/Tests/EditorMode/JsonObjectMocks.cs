using System;
using System.Collections.Generic;
using Light_Migrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Light_Migrations.Tests.EditorMode
{
    public static class MethodCallHandler
    {
        public static IReadOnlyDictionary<Type, MethodCallInfo> VersionCalledCount => _versionCalledCount;
        private static readonly Dictionary<Type, MethodCallInfo> _versionCalledCount = new();
        public static void RegisterMethodCall(Type type, string methodName)
        {
            var methodVersion = int.Parse(methodName.Split('_')[1]);
            if (_versionCalledCount.ContainsKey(type))
                _versionCalledCount[type].MethodCallCount++;
            else
                _versionCalledCount.Add(type, new MethodCallInfo(methodVersion, 1));
        }
        
        public static void Clear()
        {
            _versionCalledCount.Clear();
        }

        public class MethodCallInfo
        {
            public int methodVersion;
            public int MethodCallCount;

            public MethodCallInfo(int methodVersion, int methodCallCount)
            {
                this.methodVersion = methodVersion;
                MethodCallCount = methodCallCount;
            }
        }
    }
    
    [Migratable(1)]
    public sealed class PersonV1
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;

        private static JObject Migrate_1(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(PersonV1), nameof(Migrate_1));
            return jsonObj;
        }
    }

    [Migratable(2)]
    public sealed class PersonV2
    {
        [JsonProperty("fullName")] public string FullName;
        [JsonProperty("name")] public string Name;
        [JsonProperty("surname")] public string Surname;
        [JsonProperty("birthYear")] public int BirthYear;

        private static JObject Migrate_1(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(PersonV2), nameof(Migrate_1));
            return jsonObj;
        }

        private static JObject Migrate_2(JObject jsonObj)
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
            
            MethodCallHandler.RegisterMethodCall(typeof(PersonV2), nameof(Migrate_2));
            
            return jsonObj;
        }
    }

    [Migratable(1)]
    public sealed class PersonJsonCtor
    {
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

        private static JObject Migrate_1(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(PersonJsonCtor), nameof(Migrate_1));
            return jsonObj;
        }
    }

    public class TwoPersonsNotMigratableMock
    {
        [JsonProperty("person1")] public PersonV1 Person1;
        [JsonProperty("person2")] public PersonV1 Person2;
    }

    [Migratable(1)]
    public class TwoPersonsMigratableMock
    {
        [JsonProperty("person1")] public PersonV1 Person1;
        [JsonProperty("person2")] public PersonV1 Person2;

        private static JObject Migrate_1(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(TwoPersonsMigratableMock), nameof(Migrate_1));
            return jsonObj;
        }
    }

    public class PersonsDataStructureMock<T>
    {
        [JsonProperty("persons")] public T Persons;
    }
}