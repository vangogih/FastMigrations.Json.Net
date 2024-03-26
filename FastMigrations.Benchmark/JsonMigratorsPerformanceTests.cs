using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
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

    public sealed class ComplexWeingartnerPersonBenchmarkModel
    {
        public List<WeingartnerPersonBenchmarkModel> People { get; set; }
        public DateTime Date { get; set; }
    }
    
    public sealed class ComplexFastPersonBenchmarkModel
    {
        public List<FastPersonBenchmarkModel> People { get; set; }
        public DateTime Date { get; set; }
    }

    [GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
    [MemoryDiagnoser]
    public class JsonMigratorsPerformanceTests
    {
        private const string SimpleJson = "{\"name\":\"John\",\"age\":30}";
        private const string ComplexJson = "{\"people\":[{\"name\":\"John\",\"age\":30},{\"name\":\"John\",\"age\":30},{\"name\":\"John\",\"age\":30},{\"name\":\"John\",\"age\":30},{\"name\":\"John\",\"age\":30}],\"date\":\"2021-01-01T00:00:00\"}";

        private static readonly WeingartnerPersonBenchmarkModel _weingartnerPersonObject = new WeingartnerPersonBenchmarkModel { Name = "John", Age = 30 };
        private static readonly FastPersonBenchmarkModel _fastPersonObject = new FastPersonBenchmarkModel { Name = "John", Age = 30 };

        private static readonly ComplexWeingartnerPersonBenchmarkModel _complexWeingartnerPersonObject = new ComplexWeingartnerPersonBenchmarkModel { People = new List<WeingartnerPersonBenchmarkModel> { _weingartnerPersonObject, _weingartnerPersonObject, _weingartnerPersonObject, _weingartnerPersonObject, _weingartnerPersonObject }, Date = new DateTime(2021, 1, 1) };
        private static readonly ComplexFastPersonBenchmarkModel _complexFastPersonObject = new ComplexFastPersonBenchmarkModel { People = new List<FastPersonBenchmarkModel> { _fastPersonObject, _fastPersonObject, _fastPersonObject, _fastPersonObject, _fastPersonObject }, Date = new DateTime(2021, 1, 1) };

        private JsonSerializerSettings _baseSettings;
        private JsonSerializerSettings _fastSettings;
        private JsonSerializerSettings _weingartnerSettings;

        [IterationSetup]
        public void Setup()
        {
            _baseSettings = new JsonSerializerSettings();

            var fastMigrationsConverter = new FastMigrations.Runtime.FastMigrationsConverter(MigratorMissingMethodHandling.ThrowException);
            _fastSettings = new JsonSerializerSettings { Converters = new List<JsonConverter> { fastMigrationsConverter } };

            var weingartnerMigrator = new Weingartner.Json.Migration.MigrationConverter(new HashBasedDataMigrator<JToken>(new JsonVersionUpdater()));
            _weingartnerSettings = new JsonSerializerSettings { Converters = new List<JsonConverter> { weingartnerMigrator } };
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Simple_Serialize")]
        public void Simple_Base_Serialize()
        {
            JsonConvert.SerializeObject(_weingartnerPersonObject, _baseSettings);
        }

        [Benchmark]
        [BenchmarkCategory("Simple_Serialize")]
        public void Simple_Weingartner_Serialize()
        {
            JsonConvert.SerializeObject(_weingartnerPersonObject, _weingartnerSettings);
        }

        [Benchmark]
        [BenchmarkCategory("Simple_Serialize")]
        public void Simple_FastMigrations_Serialize()
        {
            JsonConvert.SerializeObject(_fastPersonObject, _fastSettings);
        }
        
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Complex_Serialize")]
        public void Complex_Base_Serialize()
        {
            JsonConvert.SerializeObject(_complexWeingartnerPersonObject, _baseSettings);
        }

        [Benchmark]
        [BenchmarkCategory("Complex_Serialize")]
        public void Complex_Weingartner_Serialize()
        {
            JsonConvert.SerializeObject(_complexWeingartnerPersonObject, _weingartnerSettings);
        }
        
        [Benchmark]
        [BenchmarkCategory("Complex_Serialize")]
        public void Complex_FastMigrations_Serialize()
        {
            JsonConvert.SerializeObject(_complexFastPersonObject, _fastSettings);
        }

        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Simple_Deserialize")]
        public void Simple_Base_Deserialize()
        {
            JsonConvert.DeserializeObject<WeingartnerPersonBenchmarkModel>(SimpleJson, _baseSettings);
        }

        [Benchmark]
        [BenchmarkCategory("Simple_Deserialize")]
        public void Simple_Weingartner_Deserialize()
        {
            JsonConvert.DeserializeObject<WeingartnerPersonBenchmarkModel>(SimpleJson, _weingartnerSettings);
        }

        [Benchmark]
        [BenchmarkCategory("Simple_Deserialize")]
        public void Simple_FastMigrations_Deserialize()
        {
            JsonConvert.DeserializeObject<FastPersonBenchmarkModel>(SimpleJson, _fastSettings);
        }
        
        [Benchmark(Baseline = true)]
        [BenchmarkCategory("Complex_Deserialize")]
        public void Complex_Base_Deserialize()
        {
            JsonConvert.DeserializeObject<ComplexWeingartnerPersonBenchmarkModel>(ComplexJson, _baseSettings);
        }
        
        [Benchmark]
        [BenchmarkCategory("Complex_Deserialize")]
        public void Complex_Weingartner_Deserialize()
        {
            JsonConvert.DeserializeObject<ComplexWeingartnerPersonBenchmarkModel>(ComplexJson, _weingartnerSettings);
        }
        
        [Benchmark]
        [BenchmarkCategory("Complex_Deserialize")]
        public void Complex_FastMigrations_Deserialize()
        {
            JsonConvert.DeserializeObject<ComplexFastPersonBenchmarkModel>(ComplexJson, _fastSettings);
        }
    }
}