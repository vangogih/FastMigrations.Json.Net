using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Light_Migrations.Runtime
{
    public enum MigratorMissingMethodHandling
    {
        ThrowException,
        Ignore
    }

    public class LigthMigrationsConverter : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => true;

        private readonly MigratorMissingMethodHandling _methodHandling;
        private readonly HashSet<Type> _migrationInProgress = new();

        public LigthMigrationsConverter(MigratorMissingMethodHandling methodHandling)
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
                var migratableAttribute = (MigratableAttribute)valueType.GetCustomAttribute(typeof(MigratableAttribute), true);
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
                int fromVersion;

                if (!jObject.ContainsKey(MigratorConstants.VersionJsonFieldName))
                    fromVersion = MigratorConstants.DefaultVersion;
                else
                    fromVersion = jObject[MigratorConstants.VersionJsonFieldName]!.ToObject<int>();

                var migratableAttribute = (MigratableAttribute)objectType.GetCustomAttribute(typeof(MigratableAttribute), true);
                int toVersion = migratableAttribute.Version;

                for (int currVersion = fromVersion; currVersion <= toVersion; currVersion++)
                {
                    var methodName = string.Format(MigratorConstants.MigrateMethodFormat, currVersion);
                    var migrationMethod = objectType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

                    if (migrationMethod == null)
                    {
                        switch (_methodHandling)
                        {
                            case MigratorMissingMethodHandling.ThrowException: 
                                throw new MigrationException($"Migration method {methodName} not found in {objectType.Name}");
                            case MigratorMissingMethodHandling.Ignore:         
                                continue;
                        }
                    }

                    jObject = (JObject)migrationMethod!.Invoke(null, new object[] { jObject });
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
                _migrationInProgress.Remove(objectType);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            bool isMigratable = objectType.GetCustomAttribute(typeof(MigratableAttribute)) != null;
            var isInProgress = _migrationInProgress.Contains(objectType);
            return !isInProgress && isMigratable;
        }
    }

    public sealed class MigrationException : Exception
    {
        public MigrationException(string message)
            : base(message) { }
    }
}