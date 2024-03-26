using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FastMigrations.Runtime
{
    public enum MigratorMissingMethodHandling
    {
        ThrowException,
        Ignore
    }

    public class FastMigrationsConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;
        private delegate JObject MigrateMethod(JObject data);

        private readonly MigratorMissingMethodHandling _methodHandling;
        private readonly HashSet<Type> _migrationInProgress = new();
        private readonly Dictionary<Type, MigratableAttribute> _attributeByTypeCache = new();
        private readonly Dictionary<Type, Dictionary<int, MigrateMethod>> _migrateMethodsByType = new();

        public FastMigrationsConverter(MigratorMissingMethodHandling methodHandling)
        {
            _methodHandling = methodHandling;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type valueType = value.GetType();

            try
            {
                if (_migrationInProgress.Contains(valueType))
                    return;

                _migrationInProgress.Add(valueType);

                var jObject = JObject.FromObject(value, serializer);
                var migratableAttribute = GetMigratableAttribute(valueType, _attributeByTypeCache);
                jObject.Add(MigratorConstants.VersionJsonFieldName, migratableAttribute.Version);
                jObject.WriteTo(writer);
            }
            finally
            {
                _migrationInProgress.Remove(valueType);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            try
            {
                if (_migrationInProgress.Contains(objectType))
                    return existingValue;

                _migrationInProgress.Add(objectType);

                var jObject = JObject.Load(reader);
                int fromVersion = MigratorConstants.DefaultVersion;

                if (jObject.ContainsKey(MigratorConstants.VersionJsonFieldName))
                    fromVersion = jObject[MigratorConstants.VersionJsonFieldName]!.ToObject<int>();

                var migratableAttribute = GetMigratableAttribute(objectType, _attributeByTypeCache);
                int toVersion = migratableAttribute.Version;

                for (int currVersion = fromVersion; currVersion <= toVersion; currVersion++)
                {
                    var migrationMethod = GetMigrateMethod(objectType, currVersion, _migrateMethodsByType);

                    if (migrationMethod == null)
                    {
                        switch (_methodHandling)
                        {
                            case MigratorMissingMethodHandling.ThrowException:
                            {
                                var methodName = string.Format(MigratorConstants.MigrateMethodFormat, currVersion);
                                throw new MigrationException($"Migration method {methodName} not found in {objectType.Name}");
                            }
                            case MigratorMissingMethodHandling.Ignore:
                            {
                                continue;
                            }
                        }
                    }

                    jObject = migrationMethod!(jObject);
                }

                if (existingValue != null && serializer.ObjectCreationHandling != ObjectCreationHandling.Replace)
                { 
                    using JsonReader jObjReader = jObject.CreateReader();
                    serializer.Populate(jObjReader, existingValue);
                    return existingValue;
                }

                return jObject.ToObject(objectType, serializer);
            }
            finally
            {
                _migrationInProgress.Remove(objectType);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            bool isMigratable = GetMigratableAttribute(objectType, _attributeByTypeCache) != null;
            return isMigratable && !_migrationInProgress.Contains(objectType);
        }

        private static MigratableAttribute GetMigratableAttribute(Type objectType, Dictionary<Type, MigratableAttribute> cache)
        {
            if (cache.TryGetValue(objectType, out MigratableAttribute attribute))
                return attribute;

            attribute = (MigratableAttribute) objectType.GetCustomAttribute(typeof(MigratableAttribute), true);
            cache[objectType] = attribute;
            return attribute;
        }
        
        private static MigrateMethod GetMigrateMethod(Type objectType, int version, Dictionary<Type, Dictionary<int, MigrateMethod>> cache)
        {
            if (!cache.TryGetValue(objectType, out Dictionary<int, MigrateMethod> methodsByVersion))
            {
                methodsByVersion = new Dictionary<int, MigrateMethod>();
                cache[objectType] = methodsByVersion;
            }

            if (methodsByVersion.TryGetValue(version, out MigrateMethod method))
                return method;

            var methodName = string.Format(MigratorConstants.MigrateMethodFormat, version);
            var methodInfo = objectType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

            if (methodInfo == null)
                return null;

            MigrateMethod newMethodDelegate = (MigrateMethod) methodInfo.CreateDelegate(typeof(MigrateMethod));
            methodsByVersion[version] = newMethodDelegate;
            return newMethodDelegate;
        }
    }

    public sealed class MigrationException : Exception
    {
        public MigrationException(string message)
            : base(message) { }
    }
}