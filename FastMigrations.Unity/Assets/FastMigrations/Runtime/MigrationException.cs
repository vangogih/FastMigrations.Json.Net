using System;

namespace FastMigrations.Runtime
{
    public sealed class MigrationException : Exception
    {
        public MigrationException(string message)
            : base(message) { }
    }
}