using System;

namespace Light_Migrations.Runtime
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
    public class MigratableAttribute : UnityEngine.Scripting.PreserveAttribute
    {
        public int Version { get; }

        public MigratableAttribute(int version)
        {
            Version = version;
        }
    }
}