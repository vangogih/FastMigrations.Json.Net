namespace FastMigrations.Runtime
{
    internal static class MigratorConstants
    {
        public const string VersionJsonFieldName = "JsonVersion";
        public const string MigrateMethodFormat = "Migrate_{0}";
        public const int DefaultVersion = 0;
        public const int MinVersionToStartMigration = 1;
    }
}