using Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GenerateLuisData
{
    public class Program
    {
        private static void Main(string[] args)
        {
            var doc = Generate();
            var file = OpenFile(doc);
            SaveFile(doc, file);
        }

        private static class Intents
        {
            public static string Call = "call";
            public static string Message = "message";
            public static string Read = "read";
            public static string Watch = "watch";

            public static List<string> All = new List<string> { Call, Message, Read, Watch };
        }

        private static class Entities
        {
            public static string Contact = "contact";
            //public static string FullName = "fullname";
            public static string Book = "book";
            public static string Movie = "movie";

            public static List<string> All = new List<string> { Contact, /*FullName,*/ Book, Movie };

            public static Entity ToEntity(string entityName)
            {
                var entity = new Entity { name = entityName };
                //if (entityName == FullName)
                //{
                //    entity.children = new[] { "First", "Last" };
                //}
                return entity;
            }
        }

        public enum Part
        {
            Preface,
            Middle,
            Trailer,
            Intent,
            Entity
        }

        public static IDictionary<string, List<Part>> patterns = new Dictionary<string, List<Part>>
            {
                { "{intent} {entity}", new List<Part>() {Part.Intent, Part.Entity} },
                { "{entity} {intent}", new List<Part>() {Part.Entity, Part.Intent} },
                { "{intent} {entity} {trailer}", new List<Part>() { Part.Intent, Part.Entity, Part.Trailer } },
                { "{preface} {intent} {entity}", new List<Part>() {Part.Preface, Part.Intent, Part.Entity} },
                { "{preface} {intent} {entity} {trailer}", new List<Part>() { Part.Preface, Part.Intent, Part.Entity, Part.Trailer } },
                { "{intent} {middle} {entity}", new List<Part>() { Part.Intent, Part.Middle, Part.Entity } },
                { "{intent} {middle} {entity} {trailer}", new List<Part>() { Part.Intent, Part.Middle, Part.Entity, Part.Trailer } },
                { "{preface} {entity} {middle} {intent}", new List<Part>() { Part.Preface, Part.Entity, Part.Middle, Part.Intent } },
                { "{preface} {intent} {middle} {entity}", new List<Part>() { Part.Preface, Part.Intent, Part.Middle, Part.Entity } },
                { "{preface} {intent} {middle} {entity} {trailer}", new List<Part>() {Part.Preface, Part.Intent, Part.Middle, Part.Entity, Part.Trailer} }
            };

        private static IDictionary<Part, List<string>> partOptions = new Dictionary<Part, List<string>>
                                                                    {
                                                                            { Part.Preface, new List<string>() { "make", "do", "finish", "set", "complete", "start", "continue" } },
                                                                            { Part.Middle, new List<string>() { "to", "with", "by", "along", "for" } },
                                                                            { Part.Trailer, new List<string>() { "again", "tomorrow", "today", "first", "last", "later" } }
                                                                    };

        private class IntentSynonyms : Dictionary<string, string> { }

        private static LuisDoc Generate()
        {
            var advancedNames = GetAdvancedNames();
            var simpleNames = GetSimpleNames();
            var books = GetBooks();
            var movies = GetMovies();

            return Generate(advancedNames, simpleNames, movies, books);
        }

        private static LuisDoc Generate(IEnumerable<string> advancedNames, IEnumerable<string> names, IEnumerable<string> movies, IEnumerable<string> books)
        {
            return new LuisDoc()
            {
                luis_schema_version = "2.1.0",
                versionId = "0.2.7",
                culture = "en-us",
                desc = "training data",
                name = "my-radish",
                entities = Entities.All.Select(o => Entities.ToEntity(o)).ToList(),
                intents = Intents.All.Select(o => new Intent { name = o }).ToList(),
                utterances = CreateUtterances(advancedNames, names, movies, books),
                model_features = CreateSynonyms().ToArray()
            };
        }

        private static IEnumerable<ModelFeature> CreateSynonyms()
        {
            return new List<ModelFeature>
            {
                ModelFeature.Create("Call_Phrase_List", "contact,calling,calls,connect,phone,sms,fax,mobile,voice,text,texts,call"),
                ModelFeature.Create("Email_Phrase_List", "email,e mail,mail,electronic mail,mails,emails,emailing,e - mails,e - mail,message,e mails"),
                ModelFeature.Create("Watch_Phrase_List", "watch,watching,check out,see,view,go watch,go see"),
                ModelFeature.Create("Read_Phrase_List", "read,reading,study,research,reader,reads,readers")
            };
        }

        private static IEnumerable<string> GetAdvancedNames() => GetLines(@"../../advanced-names.dat");

        private static IEnumerable<string> GetSimpleNames() => GetLines(@"../../names.dat").ToList();

        private static IEnumerable<string> GetBooks() => GetLines(@"../../books.dat");

        private static IEnumerable<string> GetMovies() => GetLines(@"../../movies.dat");

        private static IEnumerable<string> GetLines(string path)
        {
            var file = File.OpenRead(path);
            using (var reader = new StreamReader(file))
            {
                string line = string.Empty;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }

        private static List<Utterance> CreateUtterances(IEnumerable<string> advancedNames, IEnumerable<string> simpleNames, IEnumerable<string> movies, IEnumerable<string> books)
        {
            var simpleCall = CreateUtterances(Intents.Call, simpleNames, Entities.Contact);
            //var advancedCall = CreateUtterances(Intents.Call, advancedNames, Entities.FullName);
            var message = CreateUtterances(Intents.Message, simpleNames, Entities.Contact);
            var watch = CreateUtterances(Intents.Watch, movies, Entities.Movie);
            var read = CreateUtterances(Intents.Read, books, Entities.Book);

            const int maxUtterances = 10000;
            var maxUtterancesPerIntent = maxUtterances / Intents.All.Count;

            simpleCall = PickRandom(simpleCall.ToList(), Math.Min(maxUtterancesPerIntent, simpleCall.Count()));
            //advancedCall = PickRandom(advancedCall.ToList(), Math.Min(maxUtterancesPerIntent, advancedCall.Count()));
            message = PickRandom(message.ToList(), Math.Min(maxUtterancesPerIntent, message.Count()));
            watch = PickRandom(watch.ToList(), Math.Min(maxUtterancesPerIntent, watch.Count()));
            read = PickRandom(read.ToList(), Math.Min(maxUtterancesPerIntent, read.Count()));
        
            var utterances = new List<Utterance>();
            utterances.AddRange(simpleCall);
            //utterances.AddRange(advancedCall);
            utterances.AddRange(message);
            utterances.AddRange(watch);
            utterances.AddRange(read);
            return utterances;
        }

        private static IEnumerable<Utterance> CreateUtterances(string intent, IEnumerable<string> entities, string entityType)
        {
                foreach (var entity in entities)
                {
                    foreach (var pattern in patterns.Keys)
                    {
                        var text = CreateTextFromPattern(pattern, patterns[pattern], intent, entity);
                        yield return Utterance.Create(text, intent, entity, entityType);
                    }
                }
        }
        
        //TODO: spoof the intent name a little bit on some

        public static string CreateTextFromPattern(string patternFormat, List<Part> parts, string intent, string entity)
        {
            var stringList = new List<string>();
            foreach (var part in parts)
            {
                if (part == Part.Intent)
                {
                    stringList.Add(intent);
                }
                else if (part == Part.Entity)
                {
                    stringList.Add(entity);
                }
                else
                {
                    var options = partOptions[part];
                    stringList.Add(PickRandom(options));
                }
            }

            return string.Join(" ", stringList);
        }

        private static List<T> PickRandom<T>(List<T> collection, int count)
        {
            var list = new List<T>();
            for (var i = 0; i < count; i++)
            {
                var element = PickRandom(collection);
                list.Add(element);
                collection.Remove(element);
            }
            return list;
        }

        private static T PickRandom<T>(IEnumerable<T> collection)
        {
            var rand = new Random();
            return collection.ElementAt(rand.Next(0, collection.Count()));
        }

        private static FileStream OpenFile(LuisDoc doc)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var fileName = $"luis-training-data-v{doc.versionId}.json";
            var path = Path.Combine(folder, fileName);
            return File.OpenWrite(path);
        }

        private static void SaveFile(LuisDoc doc, FileStream file)
        {
            new Json().Serialize(file, doc);
        }
    }
}