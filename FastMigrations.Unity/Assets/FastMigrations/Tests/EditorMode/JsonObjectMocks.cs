using System;
using System.Collections.Generic;
using FastMigrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FastMigrations.Tests.EditorMode
{
    public static class MethodCallHandler
    {
        public static IReadOnlyDictionary<Type, MethodsCallInfo> MethodsCallInfoByType => m_methodsCallInfoByType;
        private static readonly Dictionary<Type, MethodsCallInfo> m_methodsCallInfoByType = new Dictionary<Type, MethodsCallInfo>();

        public static void RegisterMethodCall(Type type, string methodName)
        {
            var methodVersion = int.Parse(methodName.Split('_')[1]);

            if (m_methodsCallInfoByType.ContainsKey(type))
                m_methodsCallInfoByType[type].VersionsCalled.Add(methodVersion);
            else
                m_methodsCallInfoByType.Add(type, new MethodsCallInfo(methodVersion));
        }

        public static void Clear()
        {
            m_methodsCallInfoByType.Clear();
        }

        public class MethodsCallInfo
        {
            public readonly List<int> VersionsCalled;
            public int MethodCallCount => VersionsCalled.Count;

            public MethodsCallInfo(int methodVersions)
            {
                VersionsCalled = new List<int> { methodVersions };
            }
        }
    }

    [Migratable(0)]
    public sealed class PersonV0WithoutMigrateMethod
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;
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

    [Migratable(1)]
    public struct PersonStructV1
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;

        private static JObject Migrate_1(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(PersonStructV1), nameof(Migrate_1));
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
            jsonObj.Add("birthYear", 2024 - ageToken.ToObject<int>());
            jsonObj.Remove("age");

            var oldNameSplit = nameToken.ToObject<string>().Split(' ');
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
        [NonSerialized] public readonly bool IsJsonCtorCalled;

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

    [Migratable(1)]
    public sealed class PersonV1WithoutMigrationMethod
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;
    }

    public class TwoPersonsV1NotMigratableMock
    {
        [JsonProperty("person1")] public PersonV1 Person1;
        [JsonProperty("person2")] public PersonV1 Person2;
    }

    public class TwoPersonsTwoDifferentMigratableMock
    {
        [JsonProperty("person1")] public PersonV1 Person1;
        public Version Version;
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

    [Migratable(2)]
    public class ParentMock
    {
        protected static JObject Migrate_1(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(ParentMock), nameof(Migrate_1));
            return jsonObj;
        }

        protected static JObject Migrate_2(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(ParentMock), nameof(Migrate_2));
            return jsonObj;
        }
    }

    [Migratable(10)]
    public class ChildV10Mock : ParentMock
    {
        private static JObject Migrate_1(JObject jsonObj)
        {
            jsonObj = ParentMock.Migrate_1(jsonObj);
            MethodCallHandler.RegisterMethodCall(typeof(ChildV10Mock), nameof(Migrate_1));
            return jsonObj;
        }

        private static JObject Migrate_2(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(ChildV10Mock), nameof(Migrate_2));
            return jsonObj;
        }

        private static JObject Migrate_10(JObject jsonObj)
        {
            jsonObj = ParentMock.Migrate_2(jsonObj);
            MethodCallHandler.RegisterMethodCall(typeof(ChildV10Mock), nameof(Migrate_10));
            return jsonObj;
        }
    }

    [Migratable(1)]
    public class PersonV1NestedMock
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("child")] public PersonV1NestedMock Child;

        private static JObject Migrate_1(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(PersonV1NestedMock), nameof(Migrate_1));
            return jsonObj;
        }
    }

    [Migratable(1)]
    public class Directory
    {
        public string Name { get; set; }
        public Directory Parent { get; set; }
        public IList<File> Files { get; set; }

        private static JObject Migrate_1(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(Directory), nameof(Migrate_1));
            return jsonObj;
        }
    }

    [Migratable(1)]
    public class File
    {
        public string Name { get; set; }
        public Directory Parent { get; set; }

        private static JObject Migrate_1(JObject jsonObj)
        {
            MethodCallHandler.RegisterMethodCall(typeof(File), nameof(Migrate_1));
            return jsonObj;
        }
    }
}