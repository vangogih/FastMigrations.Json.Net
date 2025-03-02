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

    /*
     * Operational complexity (Co) = 130 + 28 + 392 + 12228 + 514306 + 23 + 106 = 527213
     * Architectural complexity (Ca) = 2 + 3 + 6 + 9 + 8 + 5 + 9 + 6 = 48
     * Cognitive complexity = 527213 * 48 = 25306224
     */
    public class FastMigrationsConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        private readonly MigratorMissingMethodHandling _methodHandling;

        private readonly ThreadLocal<HashSet<Type>> _migrationInProgress;
        private readonly IDictionary<Type, MigratableAttribute> _attributeByTypeCache;
        private readonly IDictionary<Type, IDictionary<int, MigrateMethod>> _migrateMethodsByType;
        
        /*
         * Operational complexity (Co) = 113 + 8 + 8 + = 130
         * Architectural complexity (Ca) = 1 + 1 = 2
         * Cognitive complexity = 130 * 2 = 260
         */
        public FastMigrationsConverter(MigratorMissingMethodHandling methodHandling)
        {
            _migrationInProgress = new ThreadLocal<HashSet<Type>>(() => // w = 1, seq = 1, func = 7 * 16 = 112, W = 1 * (1 + 112) = 113
            {
	            return new HashSet<Type>(); // w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16 
            });
            _attributeByTypeCache = new ConcurrentDictionary<Type, MigratableAttribute>(); // w = 1, seq = 1, func = 7, W = 1 + 7 = 8
            _migrateMethodsByType = new ConcurrentDictionary<Type, IDictionary<int, MigrateMethod>>(); // w = 1, seq = 1, func = 7, W = 1 + 7 = 8

            _methodHandling = methodHandling; // w = 1, seq = 1, W = 1
        }

        /*
         * Operational complexity (Co) = 8 + 6 + 6 + 8 = 28
         * Architectural complexity (Ca) = 1 + 1 + 1 = 3
         * Cognitive complexity = 28 * 3 = 84
         */
        public override bool CanConvert(Type objectType)
        {
            MigratableAttribute attribute = GetMigratableAttribute(objectType, _attributeByTypeCache); //w = 1, seq = 1, func = 7, W = 1* (1 + 7) = 8

            if (attribute == null) // w = 1, if = 3 * 2 = 6, W = 6 
                return false; // w = 2, seq = 1, W = 2 * 1 = 2 

            if (attribute.Version == MigratorConstants.DefaultVersion) // w = 1, if = 3 * 2 = 6, W = 6
                return false; // w = 2, seq = 1, W = 2 * 1 = 2

            return !_migrationInProgress.Value.Contains(objectType); // w = 1, seq = 1, func = 7, W = 1 * (1 + 7) = 8
        }

        /*
         * Operational complexity (Co) = 8 + 384 = 392 
         * Architectural complexity (Ca) = 3 + 3 = 6
         * Cognitive complexity = 392 * 6 = 2352
         */
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type valueType = value.GetType(); //w = 1, seq = 1, func = 7, W = 1 * 1 + 7 = 8

            try // w = 1, try = 3 * (112 + 16) = 384
            {
                if (_migrationInProgress.Value.Contains(valueType)) // w = 2, if = 3 * 3 = 9, func = 7, W = 2 * (9 + 7) = 32
                    return; // w = 3, seq = 1, W = 3

                _migrationInProgress.Value.Add(valueType); // w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16

                var jObject = JObject.FromObject(value, serializer); // w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
                var migratableAttribute = GetMigratableAttribute(valueType, _attributeByTypeCache); // w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
                jObject.Add(MigratorConstants.VersionJsonFieldName, migratableAttribute.Version); // w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
                jObject.WriteTo(writer); // w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
            }
            finally
            {
                _migrationInProgress.Value.Remove(valueType); // w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
            }
        }

        /*
         * Operational complexity = 12228
         * Architectural complexity = inputs + outputs + variables = 4 + 1 + 4 + 9
         * Cognitive complexity = Oc * Ac = 12228 * 9 = 100052
         */
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            try // w = 2, if = 3 * (4074+2), W = 12228
            {
                if (_migrationInProgress.Value.Contains(objectType)) // w = 2, if = 3 * 3 = 9, func = 7, W = 2*(9+7) = 32
                    return existingValue; // w = 3 * seq = 3

                _migrationInProgress.Value.Add(objectType); // w = 2, seq = 1, func = 7, W = 2*(1+7)=16

                var jObject = JObject.Load(reader); // w = 2, seq = 1, func = 7, W = 2*(1+7)=16

                //don't try and repeat migration for objects serialized as refs to previous
                if (jObject["$ref"] != null) // w = 2, if = 3 * 288 = 864, W = 2 * 864 = 1728                                                   
                    if (serializer.ReferenceResolver != null) // w = 3, if = 3 * (28 + 4) = 96, W = 3 * 96 = 288
                        return serializer.ReferenceResolver.ResolveReference(serializer, (string)jObject["$ref"]); // w = 4, func = 7, W = 28
                    else
                        return null; // w = 4, seq = 1, W = 4

                int fromVersion = MigratorConstants.DefaultVersion; // w = 2, seq = 1, W = 2

                if (jObject.ContainsKey(MigratorConstants.VersionJsonFieldName)) // w = 2, if = 3 * 24 = 72, func = 7, W = 2*(72+7) = 158
                    fromVersion = jObject[MigratorConstants.VersionJsonFieldName].ToObject<int>(); // w = 3, seq = 1, func = 7, W = 3*(1+7)=24

                var migratableAttribute = GetMigratableAttribute(objectType, _attributeByTypeCache); // w = 2, seq = 1, func = 7, W = 2*(1+7)=16
                uint toVersion = migratableAttribute.Version;                                        // w = 2, seq = 1, W = 2

                if (toVersion + fromVersion != 0 && fromVersion != toVersion)                              // w = 2, if = 3 * 24 = 72, W = 2*72 = 144
                    jObject = RunMigrations(jObject, objectType, fromVersion, toVersion, _methodHandling); // w = 3, seq = 1, func = 7, W = 3*(1+7)=24

                if (existingValue != null && serializer.ObjectCreationHandling != ObjectCreationHandling.Replace) // w=2, if = 3 * 324 = 972, W = 972 * 2 = 1944
                {
                    using (JsonReader jObjReader = jObject.CreateReader()) // w = 3, if = 3 * (32+4) = 108, W = 3 * 108 = 324
                    {
                        serializer.Populate(jObjReader, existingValue); // w = 4, seq, func, W = 4*(1+7)=32
                        return existingValue; // w = 4, seq, W = 4
                    }
                }

                return jObject.ToObject(objectType, serializer); // w = 2, seq = 1, func = 7, W = 2*(1+7)=16
            }
            finally
            {
                _migrationInProgress.Value.Remove(objectType); // w = 2, seq, W = 2
            }
        }

        /*
         * Operational complexity (Co) = 1 + 514304 + 1 = 514306
         * Architectural complexity (Ca) = 5 + 1 + 2 = 8
         * Cognitive complexity = 514306 * 8 = 4114448
         */
        private JObject RunMigrations(JObject jObject, Type objectType, int fromVersion,
            uint toVersion, MigratorMissingMethodHandling methodHandling)
        {
            fromVersion += MigratorConstants.MinVersionToStartMigration; //w = 1, seq = 1, W = 1

            for (int currVersion = fromVersion; currVersion <= toVersion; ++currVersion) // w = 1, for = 7 * (16 + 73 440 + 16), W = 514 304
            {
                var migrationMethod = GetMigrateMethod(objectType, currVersion, _migrateMethodsByType); // w = 2, seq = 1 func = 7, W = 2 * (1 + 7) = 16 

                if (migrationMethod == null) // w = 2, if = 3 * 12240 = 36720, W = 2 * 36720 = 73440
                {
                    switch (methodHandling) // w = 3, switch = 4 * (960 + 60) = 4080, W = 3 * 4080 = 12240
                    {
                        case MigratorMissingMethodHandling.ThrowException: // w = 5, case = 3 * 80 = 240, W = 4 * 240 = 960
                        {
                            var methodName = string.Format(MigratorConstants.MigrateMethodFormat, currVersion); // w = 5, seq = 1, func = 7, W = 5 * (1 + 7) = 40
                            throw new MigrationException($"Migration method {methodName} not found in {objectType.Name}"); // w = 5, seq = 1, func = 7, W = 5 * (1 + 7) = 40
                        }
                        case MigratorMissingMethodHandling.Ignore: // w = 5, case = 3 * 5 = 15, W = 4 * 15 = 60 
                        {
                            continue; // w = 5, seq = 1, W = 5
                        }
                    }
                }

                jObject = migrationMethod(jObject); // w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
            }
            return jObject; // w = 1, seq = 1, W = 1
        }

        /*
         * Operational complexity (Co) = 13 + 8 + 1 + 1 = 23
         * Architectural complexity (Ca) = 1 + 3 + 1 = 5
         * Cognitive complexity = 23 * 5 = 115
         */
        private static MigratableAttribute GetMigratableAttribute(Type objectType, IDictionary<Type, MigratableAttribute> cache)
        {
            if (cache.TryGetValue(objectType, out MigratableAttribute attribute)) //w = 1, if = 3 * 2 = 6, func = 7, W = 1 * (6 + 7) = 13
                return attribute; //w = 2, W = 2

            attribute = (MigratableAttribute)objectType.GetCustomAttribute(typeof(MigratableAttribute), false); //w = 1, seq = 1, func = 7, W = 1 * (1+7) = 8
            cache[objectType] = attribute; //w = 1, seq = 1, W = 1
            return attribute; //w = 1, seq = 1, W = 1
        }

        /*
         * Operational complexity (Co) = 61 + 13 + 8 + 8 + 6 + 8 + 1 + 1 = 106
         * Architectural complexity (Ca) = 1 + 1 + 3 + 1 + 3 = 9
         * Cognitive complexity = 106 * 9 = 954
         */
        private static MigrateMethod GetMigrateMethod(Type objectType, int version, IDictionary<Type, IDictionary<int, MigrateMethod>> cache)
        {
            if (!cache.TryGetValue(objectType, out IDictionary<int, MigrateMethod> methodsByVersion)) // w = 1, if = 3 * (16 + 2) = 54, func = 7, W = 54 + 7 = 61
            {
                methodsByVersion = new ConcurrentDictionary<int, MigrateMethod>(); // w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
                cache[objectType] = methodsByVersion; // w = 2, seq = 1, W = 2
            }

            if (methodsByVersion.TryGetValue(version, out MigrateMethod method)) // w = 1, if = 3 * 2 = 6, func = 7, W = 6 + 7 = 13
                return method; // w = 2, seq = 1, W = 2

            var methodName = string.Format(MigratorConstants.MigrateMethodFormat, version); // w = 1, seq = 1, func = 7, W = 1 + 7 = 8
            var methodInfo = objectType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic); // w = 1, seq = 1, func = 7, W = 1 + 7 = 8

            if (methodInfo == null) // w = 1, if = 3 * 2 = 6, W = 6
                return null; // w = 2, seq = 1, W = 2

            MigrateMethod newMethodDelegate = (MigrateMethod)methodInfo.CreateDelegate(typeof(MigrateMethod)); // w = 1, seq = 1, func = 7, W = 1 + 7 = 8
            methodsByVersion[version] = newMethodDelegate; // w = 1, seq = 1, W = 1
            return newMethodDelegate; // w = 1, seq = 1, W = 1
        }
    }
}