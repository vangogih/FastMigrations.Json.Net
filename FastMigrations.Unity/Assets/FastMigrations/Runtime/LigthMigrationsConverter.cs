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
     * Operational complexity (Co) = 130 + 30 + 398 + 12228 + 24520 + 23 + 109 = 37 438
     * Architectural complexity (Ca) = inputs + outputs + variables = 0 + 0 + 6 = 6
     * Cognitive complexity = Co * Ca = Не совсем понятно:
     * 1. Общая по всем методам 260 + 150 + 2388 + 100052 + 220680 + 115 + 1090 = 324 735
     * или
     * 2. 37438 * 6 = 224 628
     */
    public class FastMigrationsConverter : JsonConverter
    {
        public override bool CanRead => true; //seq = 1
        public override bool CanWrite => true; //seq = 1

        private readonly MigratorMissingMethodHandling _methodHandling; //seq = 1

        private readonly ThreadLocal<HashSet<Type>> _migrationInProgress; //seq = 1
        private readonly IDictionary<Type, MigratableAttribute> _attributeByTypeCache; //seq = 1
        private readonly IDictionary<Type, IDictionary<int, MigrateMethod>> _migrateMethodsByType; //seq = 1
        
        /*
         * Operational complexity (Co) = 113 + 8 + 8 + 1 = 130
         * Architectural complexity (Ca) = inputs + outputs + variables = 1 + 1 + 0 = 2
         * Cognitive complexity = Co * Ca = 6 * 18 = 260
         */
        public FastMigrationsConverter(MigratorMissingMethodHandling methodHandling)
        {
            _migrationInProgress = new ThreadLocal<HashSet<Type>>(() => 
                new HashSet<Type>() //w = 2, seq = 1, func = 7, W = 2 * (7 +1) = 16
            ); //w = 1, seq = 1, func = 7 * 16 = 112, W = 7 * (1 + 112) = 113
            
            _attributeByTypeCache = new ConcurrentDictionary<Type, MigratableAttribute>(); //w = 1, seq = 1, func = 7, W = 1 + 7 = 8
            _migrateMethodsByType = new ConcurrentDictionary<Type, IDictionary<int, MigrateMethod>>(); //w = 1, seq = 1, func = 7, W = 1+7 = 8

            _methodHandling = methodHandling; //w = 1, seq = 1
        }

        /*
         * Operational complexity (Co) = 30
         * Architectural complexity (Ca) = inputs + outputs + variables = 1 + 1 + 1 = 3
         * Cognitive complexity = Co * Ca = 150
         */
        public override bool CanConvert(Type objectType)
        {
            MigratableAttribute attribute = GetMigratableAttribute(objectType, _attributeByTypeCache); //w = 1, seq = 1, func = 7, W = 1 + 7 = 8

            if (attribute == null) //w = 1, seq = 1, if = 3 * 2 = 6, W = 1 * (1 + 6) = 7
                return false; //w = 2, seq = 1, W = 2 * 1 = 2

            if (attribute.Version == MigratorConstants.DefaultVersion) //w = 1, seq = 1, if = 3 * 2, W = 1 * (1 + 6) = 7
                return false; //w = 2, seq = 1, W = 2 * 1 = 2

            return !_migrationInProgress.Value.Contains(objectType); //w = 1, seq = 1, func = 7, W = 7 + 1 = 8
        }

        /*
         * Operational complexity (Co) = 390+8 = 398
         * Architectural complexity (Ca) = inputs + outputs + variables = 3 + 0 + 3 = 6
         * Cognitive complexity = Co * Ca = 148 * 6 = 2388 
         */
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type valueType = value.GetType(); //w = 1, seq = 1, func = 7, W =  1 * (1 + 7) = 8

            try //w =2, if = 3 * (114+16) = 390
            {
                if (_migrationInProgress.Value.Contains(valueType)) //w = 2, seq = 1, if = 3 * 3 = 9, func = 7, W = 2 * (1 + 9 + 7) = 34
                    return; //w = 3, seq = 1, W = 3 * 1

                _migrationInProgress.Value.Add(valueType); //w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16

                var jObject = JObject.FromObject(value, serializer); //w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
                var migratableAttribute = GetMigratableAttribute(valueType, _attributeByTypeCache); //w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
                jObject.Add(MigratorConstants.VersionJsonFieldName, migratableAttribute.Version); //w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
                jObject.WriteTo(writer); //w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
            }
            finally
            {
                _migrationInProgress.Value.Remove(valueType); //w = 2, seq = 1, func = 7, W = 2 * (1 + 7) = 16
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
         * Operational complexity (Co) = 1 + 24518 + 1 = 24520
         * Architectural complexity (Ca) = inputs + outputs + variables = 5+1+3 = 9
         * Cognitive complexity = Co * Ca = 220680
         */
        private JObject RunMigrations(JObject jObject, Type objectType, int fromVersion,
            uint toVersion, MigratorMissingMethodHandling methodHandling)
        {
            fromVersion += MigratorConstants.MinVersionToStartMigration; //w =1, seq = 1, W =1

            for (int currVersion = fromVersion; currVersion <= toVersion; ++currVersion) //w =1 , seq = 1, for = 7 * (16 + 24500 +  2) = 24518
            {
                var migrationMethod = GetMigrateMethod(objectType, currVersion, _migrateMethodsByType); //w =2 , seq = 1, func = 7, W = 2 * (7+1) = 16

                if (migrationMethod == null) //w =2 , seq = 1, if = 3 * 4083 = 12249, W = 2 * (12249 +1) = 24500
                {
                    switch (methodHandling) //w =3 , seq = 1, switch = 4 * (320 + 20) = 1360, W = 3 * ( 1360 + 1) = 4083
                    {
                        case MigratorMissingMethodHandling.ThrowException: //w =4 , seq = 1, W = 4* (40 +40) = 320
                        {
                            var methodName = string.Format(MigratorConstants.MigrateMethodFormat, currVersion); //w =5 , seq = 1, func = 7, W = 5 * (7+1) = 40
                            throw new MigrationException($"Migration method {methodName} not found in {objectType.Name}"); //w =5 , seq = 1, func = 7, W = 5 * (7+1) = 40
                        }
                        case MigratorMissingMethodHandling.Ignore: //w =4 , seq = 1, W = 4 * 5 = 20
                        {
                            continue; //w = 5, seq = 1, W = 5
                        }
                    }
                }

                jObject = migrationMethod(jObject); //w = 2, seq = 1, W = 2
            }
            return jObject; //w = 1, seq = 1, W = 1
        }

        /*
         * Operational complexity (Co) = 23
         * Architectural complexity (Ca) = inputs + outputs + variables = 2 + 2 + 1 = 5
         * Cognitive complexity = Co * Ca = 115
         */
        private static MigratableAttribute GetMigratableAttribute(Type objectType, IDictionary<Type, MigratableAttribute> cache)
        {
            if (cache.TryGetValue(objectType, out MigratableAttribute attribute)) //w = 1, seq = 1, if = 3 * 2 = 6, func = 7, W = 1 * (6+7) = 13
                return attribute; //w = 2, seq = 1, W =2

            attribute = (MigratableAttribute)objectType.GetCustomAttribute(typeof(MigratableAttribute), false); //w = 1, seq = 1, func 7, W = 1 ( 1+7) = 8
            cache[objectType] = attribute;  //w = 1, seq = 1, W = 1
            return attribute; //w = 1, seq = 1, W = 1
        }

        /*
         * Operational complexity (Co) = 62 + 14 + 8 + 8 + 7 + 8 + 1 + 1 = 109
         * Architectural complexity (Ca) = inputs + outputs + variables = 3 + 2 + 5 = 10
         * Cognitive complexity = Co * Ca = 1090
         */
        private static MigrateMethod GetMigrateMethod(Type objectType, int version, IDictionary<Type, IDictionary<int, MigrateMethod>> cache)
        {
            if (!cache.TryGetValue(objectType, out IDictionary<int, MigrateMethod> methodsByVersion)) //w = 1, seq = 1, if = 3 * (16+2) = 54, func = 7, W = 1 + 54 + 7 = 62
            {
                methodsByVersion = new ConcurrentDictionary<int, MigrateMethod>(); //w = 2, seq =1, func = 7, W = 2 * (1 + 7) = 16
                cache[objectType] = methodsByVersion; //w = 2, seq =1, W = 2 * 1 = 2
            }

            if (methodsByVersion.TryGetValue(version, out MigrateMethod method)) //w = 1, seq = 1, if = 3 * 2 = 6, func = 7, W = 1 + 6 + 7 = 14
                return method; //w = 2, seq =1, W = 2 * 1 = 2

            var methodName = string.Format(MigratorConstants.MigrateMethodFormat, version); //w = 1, seq = 1, func = 7, W = 1 + 7 = 8
            var methodInfo = objectType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic); //w = 1, seq = 1, func = 7, W = 1 + 7 = 8

            if (methodInfo == null) //w = 1, seq = 1, if = 3 * 2 = 6, W = 1 + 6= 7
                return null;//w = 2, seq = 1, W = 2 * 1 = 2

            MigrateMethod newMethodDelegate = (MigrateMethod)methodInfo.CreateDelegate(typeof(MigrateMethod)); //w = 1, seq = 1, func = 7, W = 1 + 7 = 8
            methodsByVersion[version] = newMethodDelegate; //w = 1, seq = 1, W = 1
            return newMethodDelegate; //w = 1, seq = 1, W = 1
        }
    }
}