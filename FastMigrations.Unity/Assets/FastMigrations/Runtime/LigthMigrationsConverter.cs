using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FastMigrations.Runtime
{
    public enum MigratorMissingMethodHandling
    {
        ThrowException,
        Ignore
    }

    internal delegate JObject MigrateMethod(JObject data);

    public class FastMigrationsConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        private readonly MigratorMissingMethodHandling _methodHandling;

        private readonly ThreadLocal<HashSet<Type>> _migrationInProgress;
        private readonly IDictionary<Type, MigratableAttribute> _attributeByTypeCache;
        private readonly IDictionary<Type, IDictionary<int, MigrateMethod>> _migrateMethodsByType;

        public FastMigrationsConverter(MigratorMissingMethodHandling methodHandling)
        {
            _migrationInProgress = new ThreadLocal<HashSet<Type>>(() => new HashSet<Type>());
            _attributeByTypeCache = new ConcurrentDictionary<Type, MigratableAttribute>();
            _migrateMethodsByType = new ConcurrentDictionary<Type, IDictionary<int, MigrateMethod>>();

            _methodHandling = methodHandling;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type valueType = value.GetType();

            try
            {
                if (_migrationInProgress.Value!.Contains(valueType))
                    return;

                _migrationInProgress.Value!.Add(valueType);

                var jObject = JObject.FromObject(value, serializer);
                var migratableAttribute = GetMigratableAttribute(valueType, _attributeByTypeCache);
                jObject.Add(MigratorConstants.VersionJsonFieldName, migratableAttribute.Version);
                jObject.WriteTo(writer);
            }
            finally
            {
                _migrationInProgress.Value!.Remove(valueType);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            try
            {
                if (_migrationInProgress.Value!.Contains(objectType))
                    return existingValue;

                _migrationInProgress.Value!.Add(objectType);

                var jObject = JObject.Load(reader);

                //don't try and repeat migration for objects serialized as refs to previous
                if (jObject["$ref"] != null)
                    return serializer.ReferenceResolver?.ResolveReference(serializer, ((string)jObject["$ref"])!);

                int fromVersion = MigratorConstants.DefaultVersion;

                if (jObject.ContainsKey(MigratorConstants.VersionJsonFieldName))
                    fromVersion = jObject[MigratorConstants.VersionJsonFieldName]!.ToObject<int>();

                var migratableAttribute = GetMigratableAttribute(objectType, _attributeByTypeCache);
                uint toVersion = migratableAttribute.Version;

                if (toVersion + fromVersion != 0 && fromVersion != toVersion)
                    jObject = RunMigrations(jObject, objectType, fromVersion, toVersion,
                        _methodHandling);

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
                _migrationInProgress.Value!.Remove(objectType);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            bool isMigratable = GetMigratableAttribute(objectType, _attributeByTypeCache) != null;
            return isMigratable && !_migrationInProgress.Value!.Contains(objectType);
        }

        private JObject RunMigrations(JObject jObject, Type objectType, int fromVersion,
            uint toVersion, MigratorMissingMethodHandling methodHandling)
        {
            fromVersion += MigratorConstants.MinVersionToStartMigration;

            for (int currVersion = fromVersion; currVersion <= toVersion; ++currVersion)
            {
                var migrationMethod = GetMigrateMethod(objectType, currVersion, _migrateMethodsByType);

                if (migrationMethod == null)
                {
                    switch (methodHandling)
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
            return jObject;
        }

        private static MigratableAttribute GetMigratableAttribute(Type objectType, IDictionary<Type, MigratableAttribute> cache)
        {
            if (cache.TryGetValue(objectType, out MigratableAttribute attribute))
                return attribute;

            attribute = (MigratableAttribute)objectType.GetCustomAttribute(typeof(MigratableAttribute), true);
            cache[objectType] = attribute;
            return attribute;
        }

        private static MigrateMethod GetMigrateMethod(Type objectType, int version, IDictionary<Type, IDictionary<int, MigrateMethod>> cache)
        {
            if (!cache.TryGetValue(objectType, out IDictionary<int, MigrateMethod> methodsByVersion))
            {
                methodsByVersion = new ConcurrentDictionary<int, MigrateMethod>();
                cache[objectType] = methodsByVersion;
            }

            if (methodsByVersion.TryGetValue(version, out MigrateMethod method))
                return method;

            var methodName = string.Format(MigratorConstants.MigrateMethodFormat, version);
            var methodInfo = objectType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

            if (methodInfo == null)
                return null;

            MigrateMethod newMethodDelegate = (MigrateMethod)methodInfo.CreateDelegate(typeof(MigrateMethod));
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