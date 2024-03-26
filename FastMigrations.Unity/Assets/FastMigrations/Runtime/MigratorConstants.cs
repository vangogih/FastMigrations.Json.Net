namespace FastMigrations.Runtime
{
    internal static class MigratorConstants
    {
        public const string VersionJsonFieldName = "MigrationVersion";
        public const string MigrateMethodFormat = "Migrate_{0}";
        public const int DefaultVersion = 1;
    }
}