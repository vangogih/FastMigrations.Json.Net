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
     * Operational complexity (Co) = 73+22+179+894+1409+20+137+6 = 2740
     * Architectural complexity (Ca) = inputs + outputs + variables + Ca_internal = 0+0+(3+3*3)+(1+3+6+9+7+5+7) = 50
     * Cognitive complexity = Co * Ca = 2740 * 50 = 137000
     */
    public class FastMigrationsConverter : JsonConverter
    {
        public override bool CanRead => true;// seq = 1
        public override bool CanWrite => true;// seq = 1

        private readonly MigratorMissingMethodHandling _methodHandling;// seq = 1

        private readonly ThreadLocal<HashSet<Type>> _migrationInProgress;// seq = 1
        private readonly IDictionary<Type, MigratableAttribute> _attributeByTypeCache;// seq = 1
        private readonly IDictionary<Type, IDictionary<int, MigrateMethod>> _migrateMethodsByType;// seq = 1
        
        /*
         * Operational complexity (Co) = 56+8+8+1 = 73
         * Architectural complexity (Ca) = inputs + outputs + variables = 1+0+0 = 1
         * Cognitive complexity = Co * Ca = 73 * 1 = 73
         */
        public FastMigrationsConverter(MigratorMissingMethodHandling methodHandling)
        {
            _migrationInProgress = new ThreadLocal<HashSet<Type>>( //fc = 7, seq = 1, W = (7+1)*7 = 56
                () => new HashSet<Type>()); // fc = 7, W = 7
            _attributeByTypeCache = new ConcurrentDictionary<Type, MigratableAttribute>(); // fc = 7, seq = 1, W = (7+1) = 8
            _migrateMethodsByType = new ConcurrentDictionary<Type, IDictionary<int, MigrateMethod>>(); // fc = 7, seq = 1, W = (7+1) = 8

            _methodHandling = methodHandling; // seq = 1
        }

        /*
         * Operational complexity (Co) = 8+3+3+8 = 22
         * Architectural complexity (Ca) = inputs + outputs + variables = 1+1+1 = 3
         * Cognitive complexity = Co * Ca = 22 * 3 = 66
         */
        public override bool CanConvert(Type objectType)
        {
            MigratableAttribute attribute = GetMigratableAttribute(objectType, _attributeByTypeCache); // seq = 1, func = 7,, W = (1+7)=8

            if (attribute == null) //if = 3, W = 1*3 = 3
                return false; // seq = 1

            if (attribute.Version == MigratorConstants.DefaultVersion) // if = 3, W = 1*3 = 3
                return false; // seq = 1

            return !_migrationInProgress.Value.Contains(objectType); // seq = 1, fc = 7, W = (7+1)=8 
        }

        /*
         * Operational complexity (Co) = 1+157+21 = 179
         * Architectural complexity (Ca) = inputs + outputs + variables = 3+0+3 = 6
         * Cognitive complexity = Co * Ca = 179 * 6 = 1074
         */
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type valueType = value.GetType(); //seq = 1

            try // if = 3, W = 3*(10+7+8+8+7+7+1) = 157
            {
                if (_migrationInProgress.Value.Contains(valueType)) // if = 3, fc = 7, W = (3+7)*1 = 10
                    return; // seq = 1

                _migrationInProgress.Value.Add(valueType); // fc = 7

                var jObject = JObject.FromObject(value, serializer); //seq = 1, fc = 7, W = (1+7)=8
                var migratableAttribute = GetMigratableAttribute(valueType, _attributeByTypeCache); //seq = 1, fc = 7, W = (1+7)=8
                jObject.Add(MigratorConstants.VersionJsonFieldName, migratableAttribute.Version); //fc = 7
                jObject.WriteTo(writer); //fc = 7
            }
            finally //if = 3, W = 7*3 = 21
            {
                _migrationInProgress.Value.Remove(valueType); //fc = 7
            }
        }

        /*
         * Operational complexity = 873+21 = 894
         * Architectural complexity = inputs + outputs + variables = 4 + 1 + 4 + 9
         * Cognitive complexity = Oc * Ac = 894 * 9 = 8046
         */
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            try // if = 3 , W = 3*(10+7+8+72+1+80+8+1+24+72+8) = 873
            {
                if (_migrationInProgress.Value.Contains(objectType)) //  if = 3, fc = 7, W = (3+7)*1 = 10
                    return existingValue; // seq = 1

                _migrationInProgress.Value.Add(objectType); //func = 7

                var jObject = JObject.Load(reader); //seq = 1, fc = 7, W = (1+7)=8

                //don't try and repeat migration for objects serialized as refs to previous
                if (jObject["$ref"] != null) // if = 3, W = 3*(21+3) = 72                                 
                    if (serializer.ReferenceResolver != null) // if = 3, W = 3*7 = 21
                        return serializer.ReferenceResolver.ResolveReference(serializer, (string)jObject["$ref"]); // func = 7
                    else // if = 3, W = 3*1 = 3
                        return null; //  seq = 1

                int fromVersion = MigratorConstants.DefaultVersion; //  seq = 1

                if (jObject.ContainsKey(MigratorConstants.VersionJsonFieldName)) //  if = 3, fc = 7, W = (3+7)*8 = 80
                    fromVersion = jObject[MigratorConstants.VersionJsonFieldName].ToObject<int>(); //seq = 1, fc = 7, W = (1+7)=8

                var migratableAttribute = GetMigratableAttribute(objectType, _attributeByTypeCache); //seq = 1, fc = 7, W = (1+7)=8
                uint toVersion = migratableAttribute.Version;  // seq = 1

                if (toVersion + fromVersion != 0 && fromVersion != toVersion) //  if = 3, W = 3*8 = 24
                    jObject = RunMigrations(jObject, objectType, fromVersion, toVersion, _methodHandling); //seq = 1, fc = 7, W = (1+7)= 8

                if (existingValue != null && serializer.ObjectCreationHandling != ObjectCreationHandling.Replace) // if = 3 , W = 3*24 = 72
                {
                    using (JsonReader jObjReader = jObject.CreateReader()) //if = 3 , W = 3*(1+7)=24
                    {
                        serializer.Populate(jObjReader, existingValue); //fc = 7
                        return existingValue; // seq = 1
                    }
                }

                return jObject.ToObject(objectType, serializer); //seq = 1, fc = 7, W = (1+7)=8
            }
            finally //if = 3, W = 7*3 = 21
            {
                _migrationInProgress.Value.Remove(objectType); //fc = 7
            }
        }

        /*
         * Operational complexity (Co) = 1+1407+1 = 1409
         * Architectural complexity (Ca) = inputs + outputs + variables = 5+1+1 = 7
         * Cognitive complexity = Co * Ca = 1409 * 7 = 9863
         */
        private JObject RunMigrations(JObject jObject, Type objectType, int fromVersion,
            uint toVersion, MigratorMissingMethodHandling methodHandling)
        {
            fromVersion += MigratorConstants.MinVersionToStartMigration; //seq = 1

            for (int currVersion = fromVersion; currVersion <= toVersion; ++currVersion) //for = 7, W = 7*(8+195+1) = 1407
            {
                var migrationMethod = GetMigrateMethod(objectType, currVersion, _migrateMethodsByType); //seq = 1, fc = 7, W = (1+7)=8

                if (migrationMethod == null) //if = 3, W = 3*(64+1) = 195
                {
                    switch (methodHandling) // switch = 4 , W = 4*(8+7+1) = 64
                    {
                        case MigratorMissingMethodHandling.ThrowException:
                        {
                            var methodName = string.Format(MigratorConstants.MigrateMethodFormat, currVersion); //seq = 1, fc = 7, W = (1+7)=8
                            throw new MigrationException($"Migration method {methodName} not found in {objectType.Name}"); // fc = 7, W = 7
                        }
                        case MigratorMissingMethodHandling.Ignore:
                        {
                            continue;//seq = 1
                        }
                    }
                }

                jObject = migrationMethod(jObject); //seq = 1
            }
            return jObject;//seq = 1
        }

        /*
         * Operational complexity (Co) = 10+8+1+1 = 20
         * Architectural complexity (Ca) = inputs + outputs + variables = 4+1 = 5
         * Cognitive complexity = Co * Ca = 20 * 5 = 100
         */
        private static MigratableAttribute GetMigratableAttribute(Type objectType, IDictionary<Type, MigratableAttribute> cache)
        {
            if (cache.TryGetValue(objectType, out MigratableAttribute attribute)) //if = 3, fc = 7, W = (3+7)*1 = 10
                return attribute; // seq = 1

            attribute = (MigratableAttribute)objectType.GetCustomAttribute(typeof(MigratableAttribute), false); //seq = 1, fc = 7, W = (1+7)=8
            cache[objectType] = attribute; // seq = 1
            return attribute;// seq = 1
        }

        /*
         * Operational complexity (Co) = 90+8+10+8+8+3+8+1+1 = 137
         * Architectural complexity (Ca) = inputs + outputs + variables = 5+1+1 = 7
         * Cognitive complexity = Co * Ca = 137 * 7 = 959
         */
        private static MigrateMethod GetMigrateMethod(Type objectType, int version, IDictionary<Type, IDictionary<int, MigrateMethod>> cache)
        {
            if (!cache.TryGetValue(objectType, out IDictionary<int, MigrateMethod> methodsByVersion)) //if = 3, fc = 7, W = (3+7)*(8+1) = 90
            {
                methodsByVersion = new ConcurrentDictionary<int, MigrateMethod>(); // fc = 7, seq = 1, W = (7+1) = 8
                cache[objectType] = methodsByVersion; // seq = 1
            }

            if (methodsByVersion.TryGetValue(version, out MigrateMethod method)) //if = 3, fc = 7, W = 3*1+7 = 10
                return method; // seq = 1

            var methodName = string.Format(MigratorConstants.MigrateMethodFormat, version); //seq = 1, fc = 7, W = (1+7)=8
            var methodInfo = objectType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic); //seq = 1, fc = 7, W = (1+7)=8

            if (methodInfo == null) //if = 3, W = 3*1 = 3
                return null; // seq = 1

            MigrateMethod newMethodDelegate = (MigrateMethod)methodInfo.CreateDelegate(typeof(MigrateMethod));  // fc = 7, seq = 1, W = (7+1) = 8
            methodsByVersion[version] = newMethodDelegate; // seq = 1
            return newMethodDelegate; // seq = 1
        }
    }
}