using System;

namespace FastMigrations.Runtime
{
    /// <summary>
    /// All "Migrate_<see cref="MigratableAttribute.Version"/>(JObject data)" methods on object marked with this attribute will be called on deserialization.
    /// On serialization value from version field will be added to json (schema: "JsonVersion": int).
    /// </summary>
    /// <remarks>For unity this attribute inherited from "<see cref="UnityEngine.Scripting.PreserveAttribute"/>" to prevent "Migrate_<see cref="MigratableAttribute.Version"/>(JObject data)" methods deletions</remarks>
    /// <seealso cref="FastMigrationsConverter"/>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class MigratableAttribute :
#if UNITY_2019_4_OR_NEWER
        UnityEngine.Scripting.PreserveAttribute
#else
        System.Attribute
#endif
    {
        /// <summary>Number of current version of model</summary>
        /// <remarks>Starts to call Migrate_() methods from version 1. "Migrate_0()" won't be called</remarks>
        /// <seealso cref="FastMigrationsConverter"/>
        public readonly uint Version;

        /// <param name="version">Number of current version of model. Starts to call Migrate_() methods from version 1. "Migrate_0()" won't be called</param>
        /// <seealso cref="FastMigrationsConverter"/>
        public MigratableAttribute(uint version)
        {
            Version = version;
        }
    }
}