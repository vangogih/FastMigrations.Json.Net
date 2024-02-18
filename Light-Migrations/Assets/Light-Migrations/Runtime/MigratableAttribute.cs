using System;

namespace Light_Migrations.Runtime
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class MigratableAttribute : UnityEngine.Scripting.PreserveAttribute
    {
        public int Version { get; }

        public MigratableAttribute(int version)
        {
            Version = version;
        }
    }
}