using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Light_Migrations.Runtime
{
    public class Migrator : JsonConverter
    {
        private readonly Dictionary<Type, bool> _migratedTypes = new();
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            try
            {
                _migratedTypes[objectType] = true;

                var jObject = JObject.Load(reader);
                int fromVersion;

                if (!jObject.ContainsKey(MigratorConstants.VersionFieldName))
                    fromVersion = 1;
                else
                    fromVersion = jObject[MigratorConstants.VersionFieldName]!.ToObject<int>();

                var migratableAttribute = (MigratableAttribute)objectType.GetCustomAttribute(typeof(MigratableAttribute), true);
                var toVersion = migratableAttribute.Version;

                for (int currVersion = fromVersion; currVersion <= toVersion; currVersion++)
                {
                    var methodName = $"Migrate_{currVersion}";
                    var migrationMethod = objectType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

                    if (migrationMethod == null)
                        throw new MigrationException($"Implementation of migration method not found, should be private static JObject {methodName}(JObject jsonObj)");

                    jObject = (JObject)migrationMethod.Invoke(null, new object[] { jObject });
                }

                using JsonReader jObjReader = jObject.CreateReader();

                if (existingValue != null && serializer.ObjectCreationHandling != ObjectCreationHandling.Replace)
                {
                    serializer.Populate(jObjReader, existingValue);
                    return existingValue;
                }

                return serializer.Deserialize(jObjReader, objectType);
            }
            finally
            {
                _migratedTypes[objectType] = false;
            }
        }

        public override bool CanConvert(Type objectType)
        {
            bool isMigratable = objectType.GetCustomAttribute(typeof(MigratableAttribute)) != null;
            _migratedTypes.TryGetValue(objectType, out bool isMigrated);
            return !isMigrated && isMigratable;
        }
    }
    
    public class MigrationException : Exception
    {
        public MigrationException(string message) : base(message) { }
    }
}