using System;

namespace FastMigrations.Runtime
{
    /// <summary>Represents errors that occur during <see cref="FastMigrationsConverter"/></summary>
    /// <seealso cref="MigratorMissingMethodHandling"/>
    public sealed class MigrationException : Exception
    {
        ///<summary>
        /// Initializes a new instance of the MigrationException class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error</param>
        /// <seealso cref="MigratorMissingMethodHandling"/>
        public MigrationException(string message)
            : base(message) { }
    }
}