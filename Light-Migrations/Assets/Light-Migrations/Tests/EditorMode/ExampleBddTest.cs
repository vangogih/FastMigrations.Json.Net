using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Light_Migrations.Tests.EditorMode
{
    public interface IMigratable
    {
        [JsonProperty("Version", Required = Required.Always)]
        int Version { get; }

        JObject Migrate(JObject jsonObj, int from, int to);
    }

    public class PersonV1 : IMigratable
    {
        public int Version { get; private set; } = 1;
        
        [NonSerialized]
        public bool IsMigrationCalled;

        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;
        public JObject Migrate(JObject jsonObj, int from, int to)
        {
            IsMigrationCalled = true;
            return null;
        }
    }

    public sealed class Migrator : JsonConverter
    {
        public override bool CanRead => true;

        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);
            var versionToken = jObject["Version"];
            int versionValue;
            var instance = (IMigratable) Activator.CreateInstance(objectType);
            
            if (versionToken == null)
            {
                jObject.Add("Version" , instance.Version);
                versionValue = instance.Version;
            }
            else
                versionValue = versionToken.ToObject<int>();

            jObject = instance.Migrate(jObject, versionValue, instance.Version);

            return jObject.ToObject(objectType);
        }

        public override bool CanConvert(Type objectType) => objectType.IsInstanceOfType(typeof(IMigratable));
    }

    public sealed class ExampleBddTest
    {
        private JsonSerializerSettings _setting;
        private string _v1;
        private string _v2;
        private string _v3;

        [Test]
        public void Migration_Test()
        {
            // given => json with field Version
            var json = @"{""name"":""John Doe"",""age"":33,""Version"":1}";

            // when => deserialize json we run migrator
            var migrator = new Migrator();
            var person = JsonConvert.DeserializeObject<PersonV1>(json, migrator);

            // then => we call migrator method
            Assert.NotNull(person);
            Assert.IsTrue(person.IsMigrationCalled);
        }

        [TearDown]
        public void TearDown()
        {
        }

        [SetUp]
        public void SetUp()
        {
            Debug.Log(JsonConvert.SerializeObject(new PersonV1 {Age = 33, Name = "John Doe"}, _setting));
        }
    }
}