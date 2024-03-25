using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Running;
using Light_Migrations.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Weingartner.Json.Migration;

namespace Light_Migrations.Benchmark
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

    [Light_Migrations.Runtime.Migratable(1)]
    public sealed class LightPersonBenchmarkModel
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
        private static readonly LightPersonBenchmarkModel _lightPersonObject = new LightPersonBenchmarkModel { Name = "John", Age = 30 };

        private JsonSerializerSettings _lightSettings;
        private JsonSerializerSettings _weingartnerSettings;

        [IterationSetup]
        public void Setup()
        {
            var lightMigrationsConverter = new Light_Migrations.Runtime.LightMigrationsConverter(MigratorMissingMethodHandling.ThrowException);
            _lightSettings = new JsonSerializerSettings { Converters = new List<JsonConverter> { lightMigrationsConverter } };

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
        public void LightMigrations_Serialize()
        {
            JsonConvert.SerializeObject(_lightPersonObject, _lightSettings);
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
        public void LightMigrations_Deserialize()
        {
            JsonConvert.DeserializeObject<LightPersonBenchmarkModel>(PersonJson, _lightSettings);
        }

        [Benchmark]
        [BenchmarkCategory("Deserialize")]
        public void Weingartner_Deserialize()
        {
            JsonConvert.DeserializeObject<WeingartnerPersonBenchmarkModel>(PersonJson, _weingartnerSettings);
        }
    }
}