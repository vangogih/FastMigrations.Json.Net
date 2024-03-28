using System;

namespace FastMigrations.Runtime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
    public class MigratableAttribute :
#if UNITY_2018_3_OR_NEWER
        UnityEngine.Scripting.PreserveAttribute
#else 
        System.Attribute
#endif
    {
        public readonly uint Version;

        public MigratableAttribute(uint version)
        {
            Version = version;
        }
    }
}