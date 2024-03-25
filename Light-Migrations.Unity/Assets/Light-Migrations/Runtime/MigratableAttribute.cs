using System;

namespace Light_Migrations.Runtime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class MigratableAttribute : 
#if UNITY_2018_3_OR_NEWER
        UnityEngine.Scripting.PreserveAttribute
#else 
        System.Attribute
#endif
    {
        public int Version { get; }

        public MigratableAttribute(int version)
        {
            Version = version;
        }
    }
}