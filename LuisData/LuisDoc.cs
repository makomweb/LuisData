using System.Collections.Generic;

namespace GenerateLuisData
{
    public class LuisDoc
    {
        public string luis_schema_version { get; set; }
        public string versionId { get; set; }
        public string culture { get; set; }
        public string desc { get; set; }
        public string name { get; set; }
        public object[] composites = new object[] { };
        public object[] closedLists = new object[] { };
        public object[] bing_entities = new object[] { };
        public object[] actions = new object[] { };
        public object[] model_features = new object[] { };
        public object[] regex_features = new object[] { };
        public List<Intent> intents { get; set; }
        public List<Entity> entities { get; set; }
        public List<Utterance> utterances { get; set; }
    }
}