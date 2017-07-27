using Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class LuisTests
    {
        private const string LuisAddress = "https://westus.api.cognitive.microsoft.com/luis/v2.0/apps/adf3f199-d0b7-4d38-b382-3bf815d0614e?subscription-key=f4df7c06590c4eefa9caf44ce1457e8c&timezoneOffset=0&verbose=true&q=";
        private readonly HttpClient _http = new HttpClient();

        public static class DemoPropertyDataSource
        {
            private static readonly List<object[]> _data = new List<object[]>
            {
                new object[] {"Call Dennis to buy some snacks", new ExpectedResponse("call", "contact", "Dennis") },
                new object[] {"Watch Guardians of the Galaxy", new ExpectedResponse("watch", "movie", "Guardians of the Galaxy") },
                new object[] {"Message Andy for a meeting", new ExpectedResponse("message", "contact", "Andy") },
                new object[] {"Read 1984", new ExpectedResponse("read", "book", "1984") },
                new object[] {"Watch Lord Of The Rings", new ExpectedResponse("watch", "movie", "Lord Of The Rings") },
                new object[] {"Read Lord Of The Rings", new ExpectedResponse("read", "book", "Lord Of The Rings") }
            };

            public static IEnumerable<object[]> TestData
            {
                get { return _data; }
            }
        }

        [Theory]
        [MemberData("TestData", MemberType = typeof(DemoPropertyDataSource))]
        public async Task Requesting_LUIS_should_succeed(string todo, ExpectedResponse expected)
        {
            var response = await RequestAsync(todo);
            AssertResponse(response, expected);
        }

        [Fact]
        public async Task Deserialize_response_should_succeed()
        {
            var expected = new ExpectedResponse("call", "contact", "micha");
            var response = Deserialize<LuisResponse>(@"luis-response.json");
            AssertResponse(response, expected);
        }

        private void AssertResponse(LuisResponse actual, ExpectedResponse expected)
        {
            Assert.NotNull(actual);
            Assert.NotNull(expected);
            Assert.True(expected.Intent == actual.topScoringIntent.intent, "intents do not match");

            var entity = actual.entities.OrderByDescending(e => e.score).FirstOrDefault();
            Assert.True(entity != null, "entity should not be null!");
            Assert.True(expected.EntityType == entity.@type, "entity types do not match!");
            Assert.True(string.Equals(expected.EntityValue, entity.entity, StringComparison.OrdinalIgnoreCase), "entity values do not match!");
        }

        private async Task<LuisResponse> RequestAsync(string todo)
        {          
            var query = LuisAddress + todo;
            var response = await _http.GetAsync(query);
            AssertSuccess(response);
            return Deserialize(await response.Content.ReadAsStringAsync());
        }

        public class LuisResponse
        {
            public class TopScoringIntent
            {
                public string intent { get; set; }
                public double score { get; set; }
            }

            public class Intent
            {
                public string intent { get; set; }
                public double score { get; set; }
            }

            public class Entity
            {
                public string entity { get; set; }
                public string type { get; set; }
                public int startIndex { get; set; }
                public int endIndex { get; set; }
                public double score { get; set; }
            }

            public string query { get; set; }
            public TopScoringIntent topScoringIntent { get; set; }
            public List<Intent> intents { get; set; }
            public List<Entity> entities { get; set; }
        }

        public class ExpectedResponse
        {
            public ExpectedResponse(string intent, string entityType, string entityValue)
            {
                Intent = intent;
                EntityType = entityType;
                EntityValue = entityValue;
            }

            public string Intent { get; private set; }
            public string EntityType { get; private set; }
            public string EntityValue { get; private set; }
        }

        private static LuisResponse Deserialize(string json)
        {
            return new Json().Deserialize<LuisResponse>(json);
        }

        
        private static T Deserialize<T>(string name)
        {
            var text = File.ReadAllText(name);
            return new Json().Deserialize<T>(text);
        }

        private static void AssertSuccess(HttpResponseMessage message)
        {
            if (!message.IsSuccessStatusCode)
            {
                throw new Exception("HTTP request failed!");
            }
        }
    }
}
