using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Light_Migrations.Tests.EditorMode
{
    public class PersonV1
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("age")] public int Age;
    }
    
    public class PersonV2
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("surname")] public string Surname;
        [JsonProperty("age")] public int Age;
        
        // Dictionary<int, Dictionary<string, HashSet<int>>> 
        public Dictionary<Guid, List<(string, HashSet<int>)>> test;
    }

    public class Person
    {
        [JsonProperty("name")] public string Name;
        [JsonProperty("surname")] public string Surname;
        [JsonProperty("bornDate")] public DateTime BornDate;
    }

    public sealed class Migrator : JsonConverter
    {
        public override bool CanRead => true;
        public override bool CanWrite => false;

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { }

        public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue,
            JsonSerializer serializer)
        {
            if (objectType != typeof(Person))
                return existingValue;

            var jPerson = JObject.Load(reader);
            var name = jPerson["name"]!.ToObject<string>();

            string[] nameSplit = name.Split(' ');

            if (nameSplit.Length > 1)
            {
                jPerson["name"] = nameSplit[0];

                if (jPerson.Property("surname") == null)
                    jPerson.Add("surname", nameSplit[1]);
                else
                    jPerson["surname"] = nameSplit[1];
            }

            if (jPerson.Property("bornDate") == null)
            {
                var age = jPerson["age"].ToObject<int>();
                jPerson["bornDate"] = DateTime.Now.AddYears(-age);
            }

            return jPerson.ToObject<Person>();
        }

        public override bool CanConvert(System.Type objectType)
        {
            return true;
        }
    }

    public sealed class ExampleBddTest
    {
        private JsonSerializerSettings _setting;
        private string _v1;
        private string _v2;
        private string _v3;

        [Test]
        public void NotToDo()
        {
            var from = JsonConvert.DeserializeObject<PersonV1>(_v1);
            var to = new PersonV2
            { 
                Name = from.Name.Split(' ')[0], 
                Surname = from.Name.Split(' ')[1], 
                Age = from.Age 
            };
            var json = JsonConvert.SerializeObject(to);
            
            //File.WriteAllText("path", json);
        }

        [Test]
        public void Migration_v1Tov2()
        {
            Assert.DoesNotThrow(() => JsonConvert.DeserializeObject<Person>(_v1));

            // Expected
            var expectedName = "John";
            var expectedSurname = "Doe";

            // Actual
            var actual = JsonConvert.DeserializeObject<Person>(_v1, _setting);
            var actualName = actual.Name;
            var actualSurname = actual.Surname;

            Assert.AreEqual(expectedName, actualName);
            Assert.AreEqual(expectedSurname, actualSurname);
        }
        
        [Test]
        public void Migration_v2Tov3()
        {
            Assert.IsNotNull(JsonConvert.DeserializeObject<Person>(_v2));

            // Expected
            var expectedName = "John";
            var expectedSurname = "Doe";
            var expectedBurnDate = DateTime.Now.AddYears(-33);

            // Actual
            var actual = JsonConvert.DeserializeObject<Person>(_v2, _setting);
            var actualName = actual.Name;
            var actualSurname = actual.Surname;
            var actualBurnDate = actual.BornDate;

            Assert.AreEqual(expectedName, actualName);
            Assert.AreEqual(expectedSurname, actualSurname);
            Assert.AreEqual(expectedBurnDate, actualBurnDate);
        }

        [TearDown] public void TearDown() { }

        [SetUp]
        public void SetUp()
        {
            _setting = new JsonSerializerSettings { Converters = new List<JsonConverter> { new Migrator() } };

            /*
 * json schema v1
 * {
 *  "name": "string",
 *  "age": "int32"
 * }
 */
            _v1 = @"{""name"":""John Doe"",""age"":33}";

            /*
             * json schema v2
             * {
             * "name": "string",
             * "surname": "string",
             * "age": "int32"
             * }
             */

            _v2 = @"{""name"":""John"", ""surname"":""Doe"",""age"":""33""}";

            /*
             * json schema v3
             * {
             * "name": "string",
             * "surname": "string",
             * "burnDate": "DateTime"
             * }
             */

            _v3 = @"{""name"":""John"", ""surname"":""Doe"",""burnDate"":""2011-10-05 00:00:00""}";
        }
    }
}