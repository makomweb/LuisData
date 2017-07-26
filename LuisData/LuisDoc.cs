using System.Collections.Generic;

namespace GenerateLuisData
{
    public class ModelFeature
    {
        public string name { get; set; }
        public bool mode { get; set; }
        public string words { get; set; }
        public bool activated { get; set; }

        public static ModelFeature Create(string name, string commaSeparatedWords)
        {
            return new ModelFeature { name = name, words = commaSeparatedWords, mode = true, activated = true };
        }
    }

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
        public ModelFeature[] model_features = new ModelFeature[] { };
        public object[] regex_features = new object[] { };
        public List<Intent> intents { get; set; }
        public List<Entity> entities { get; set; }
        public List<Utterance> utterances { get; set; }
    }
}