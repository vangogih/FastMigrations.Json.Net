using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using FastMigrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weingartner.Json.Migration;

namespace FastMigrations.Benchmark
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<JsonMigratorsPerformanceTests>();
        }
    }

    [Weingartner.Json.Migration.Migratable("")]
    public sealed class WeingartnerPersonBenchmarkModel
    {
        private int Version = 1;
        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;

        private static JObject Migrate_1(JObject data, JsonSerializer serializer) => data;
    }

    [FastMigrations.Runtime.Migratable(1)]
    public sealed class FastPersonBenchmarkModel
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;

        private static JObject Migrate_1(JObject jsonObj) => jsonObj;
    }

    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [SimpleJob(RunStrategy.ColdStart, iterationCount: 5)]
    [MemoryDiagnoser]
    public class JsonMigratorsPerformanceTests
    {
        private const string PersonJson = "{\"name\":\"John\",\"age\":30}";
        private static readonly WeingartnerPersonBenchmarkModel _weingartnerPersonObject = new WeingartnerPersonBenchmarkModel { Name = "John", Age = 30 };
        private static readonly FastPersonBenchmarkModel _fastPersonObject = new FastPersonBenchmarkModel { Name = "John", Age = 30 };

        private JsonSerializerSettings _fastSettings;
        private JsonSerializerSettings _weingartnerSettings;

        [IterationSetup]
        public void Setup()
        {
            var fastMigrationsConverter = new FastMigrations.Runtime.FastMigrationsConverter(MigratorMissingMethodHandling.ThrowException);
            _fastSettings = new JsonSerializerSettings { Converters = new List<JsonConverter> { fastMigrationsConverter } };

            var weingartnerMigrator = new Weingartner.Json.Migration.MigrationConverter(new HashBasedDataMigrator<JToken>(new JsonVersionUpdater()));
            _weingartnerSettings = new JsonSerializerSettings { Converters = new List<JsonConverter> { weingartnerMigrator } };
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Serialize")]
        public void Base_Serialize()
        {
            JsonConvert.SerializeObject(_weingartnerPersonObject);
        }

        [Benchmark]
        [BenchmarkCategory("Serialize")]
        public void FastMigrations_Serialize()
        {
            JsonConvert.SerializeObject(_fastPersonObject, _fastSettings);
        }

        [Benchmark]
        [BenchmarkCategory("Serialize")]
        public void Weingartner_Serialize()
        {
            JsonConvert.SerializeObject(_weingartnerPersonObject, _weingartnerSettings);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Deserialize")]
        public void Base_Deserialize()
        {
            JsonConvert.DeserializeObject<WeingartnerPersonBenchmarkModel>(PersonJson);
        }
        
        [Benchmark]
        [BenchmarkCategory("Deserialize")]
        public void FastMigrations_Deserialize()
        {
            JsonConvert.DeserializeObject<FastPersonBenchmarkModel>(PersonJson, _fastSettings);
        }

        [Benchmark]
        [BenchmarkCategory("Deserialize")]
        public void Weingartner_Deserialize()
        {
            JsonConvert.DeserializeObject<WeingartnerPersonBenchmarkModel>(PersonJson, _weingartnerSettings);
        }
    }
}