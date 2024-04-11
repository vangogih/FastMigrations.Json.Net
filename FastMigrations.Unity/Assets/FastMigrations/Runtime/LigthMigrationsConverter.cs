using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FastMigrations.Runtime
{
    /// <summary>
    /// Variant of handling missing "Migrate_<see cref="MigratableAttribute.Version"/>(JObject data)" method
    /// </summary>
    /// <seealso cref="MigratorMissingMethodHandling"/>
    public enum MigratorMissingMethodHandling
    {
        /// <summary>Throws <see cref="MigrationException"/> if "Migrate_<see cref="MigratableAttribute.Version"/>(JObject data)" method doesn't exist on deserializable object</summary>
        ThrowException,
        /// <summary>Skips migration if "Migrate_<see cref="MigratableAttribute.Version"/>(JObject data)" method doesn't exist on deserializable object</summary>
        Ignore
    }

    internal delegate JObject MigrateMethod(JObject data);

    /// <summary>
    /// Thread safe JsonConverter who calls "Migrate_<see cref="MigratableAttribute.Version"/>(JObject data)" methods on objects marked with attribute <see cref="MigratableAttribute"/> on deserialization.
    ///
    /// All methods must have signature "private/protected static JObject Migrate_<see cref="MigratableAttribute.Version"/>(JObject data)".
    ///
    /// All methods will be called from current version to target version (inclusive).
    /// </summary>
    /// 
    /// <remarks>
    /// By default all classes have <see cref="MigratableAttribute.Version"/> 0.
    /// You can mark all potentially migratable objects with <see cref="MigratableAttribute"/>.
    /// </remarks>
    ///
    /// <example>
    /// Methods you have to implement in your class:
    /// <code>
    /// [Migratable(1)]
    /// public class YouObjectType
    /// {
    ///    private static JObject Migrate_1(JObject data)
    ///    // OR
    ///    protected static JObject Migrate_1(JObject data)
    ///    // !public modifier is not allowed!
    /// }
    /// </code>
    /// How to add migrator to JsonConverter:
    /// <code>
    /// var migrator = new FastMigrationsConverterMock(MigratorMissingMethodHandling.ThrowException);
    /// var person = JsonConvert.DeserializeObject&lt;YouObjectType&gt;(json, migrator);
    /// // OR
    /// JsonConvert.DefaultSettings = () => new JsonSerializerSettings
    /// {
    ///     Converters = new List&lt;JsonConverter&gt; { new FastMigrationsConverter(MigratorMissingMethodHandling.ThrowException) }
    /// };
    /// </code>
    /// </example>
    ///
    /// <exception cref="MigrationException">"Migrate_<see cref="MigratableAttribute.Version"/>(JObject data)" method doesn't exist on deserializable object</exception>
    public class FastMigrationsConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        private readonly MigratorMissingMethodHandling _methodHandling;

        private readonly ThreadLocal<HashSet<Type>> _migrationInProgress;
        private readonly IDictionary<Type, MigratableAttribute> _attributeByTypeCache;
        private readonly IDictionary<Type, IDictionary<int, MigrateMethod>> _migrateMethodsByType;

        /// <param name="methodHandling">Variant of handling missing "Migrate_<see cref="MigratableAttribute.Version"/>(JObject data)" method</param>
        /// <seealso cref="MigratorMissingMethodHandling"/>
        public FastMigrationsConverter(MigratorMissingMethodHandling methodHandling)
        {
            _migrationInProgress = new ThreadLocal<HashSet<Type>>(() => new HashSet<Type>());
            _attributeByTypeCache = new ConcurrentDictionary<Type, MigratableAttribute>();
            _migrateMethodsByType = new ConcurrentDictionary<Type, IDictionary<int, MigrateMethod>>();

            _methodHandling = methodHandling;
        }

        public override bool CanConvert(Type objectType)
        {
            MigratableAttribute attribute = GetMigratableAttribute(objectType, _attributeByTypeCache);

            if (attribute == null)
                return false;

            if (attribute.Version == MigratorConstants.DefaultVersion)
                return false;

            return !_migrationInProgress.Value.Contains(objectType);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type valueType = value.GetType();

            try
            {
                if (_migrationInProgress.Value.Contains(valueType))
                    return;

                _migrationInProgress.Value.Add(valueType);

                var jObject = JObject.FromObject(value, serializer);
                var migratableAttribute = GetMigratableAttribute(valueType, _attributeByTypeCache);
                jObject.Add(MigratorConstants.VersionJsonFieldName, migratableAttribute.Version);
                jObject.WriteTo(writer);
            }
            finally
            {
                _migrationInProgress.Value.Remove(valueType);
            }
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            try
            {
                if (_migrationInProgress.Value.Contains(objectType))
                    return existingValue;

                _migrationInProgress.Value.Add(objectType);

                var jObject = JObject.Load(reader);

                //don't try and repeat migration for objects serialized as refs to previous
                if (jObject["$ref"] != null)
                    return serializer.ReferenceResolver?.ResolveReference(serializer, (string)jObject["$ref"]);

                int fromVersion = MigratorConstants.DefaultVersion;

                if (jObject.ContainsKey(MigratorConstants.VersionJsonFieldName))
                    fromVersion = jObject[MigratorConstants.VersionJsonFieldName].ToObject<int>();

                var migratableAttribute = GetMigratableAttribute(objectType, _attributeByTypeCache);
                uint toVersion = migratableAttribute.Version;

                if (toVersion + fromVersion != 0 && fromVersion != toVersion)
                    jObject = RunMigrations(jObject, objectType, fromVersion, toVersion,
                        _methodHandling);

                if (existingValue != null && serializer.ObjectCreationHandling != ObjectCreationHandling.Replace)
                {
                    using (JsonReader jObjReader = jObject.CreateReader())
                    {
                        serializer.Populate(jObjReader, existingValue);
                        return existingValue;
                    }
                }

                return jObject.ToObject(objectType, serializer);
            }
            finally
            {
                _migrationInProgress.Value.Remove(objectType);
            }
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

                jObject = migrationMethod(jObject);
            }
            return jObject;
        }

        private static MigratableAttribute GetMigratableAttribute(Type objectType, IDictionary<Type, MigratableAttribute> cache)
        {
            if (cache.TryGetValue(objectType, out MigratableAttribute attribute))
                return attribute;

            attribute = (MigratableAttribute)objectType.GetCustomAttribute(typeof(MigratableAttribute), false);
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
}