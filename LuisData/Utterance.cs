using System.Collections.Generic;

namespace GenerateLuisData
{
    using System;

    public class Utterance
    {
        public static Utterance Create(string text, string intent, string name, string entity)
        {
            return new Utterance
            {
                text = text,
                intent = intent,
                entities = new List<Entity> { Entity.Create(text, name, entity) }
            };
        }

        public override bool Equals(Object obj)
        {
            Utterance personObj = obj as Utterance;
            if (personObj == null)
                return false;
            else
                return text.Equals(personObj.text);
        }

        public override int GetHashCode()
        {
            return this.text.GetHashCode();
        }

        public class Entity
        {
            public static Entity Create(string text, string entityText, string entity)
            {
                var startPos = text.IndexOf(entityText);
                return new Entity
                {
                    entity = entity,
                    startPos = startPos,
                    endPos = startPos + entityText.Length - 1 // -1 because counting starts at 0
                };
            }

            public string entity { get; set; }
            public int startPos { get; set; }
            public int endPos { get; set; }
        }

        public string text { get; set; }
        public string intent { get; set; }
        public List<Entity> entities { get; set; }
    }
}