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
            public static string Name = "name";
            public static string FullName = "fullname";
            public static string Book = "book";
            public static string Movie = "movie";

            public static List<string> All = new List<string> { Name, FullName, Book, Movie };

            public static Entity ToEntity(string entityName)
            {
                var entity = new Entity { name = entityName };
                if (entityName == FullName)
                {
                    entity.children = new[] { "First", "Last" };
                }
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

        private static IDictionary<Part, List<string>> noiseMap = new Dictionary<Part, List<string>>
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
                utterances = CreateUtterances(advancedNames, names, movies, books)
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
            var simpleCalls = CreateUtterances(Intents.Call, simpleNames, Entities.Name);
            var advancedCalls = CreateUtterances(Intents.Call, advancedNames, Entities.FullName);
            var message = CreateUtterances(Intents.Message, simpleNames, Entities.FullName);
            var watch = CreateUtterances(Intents.Watch, movies, Entities.Movie);
            var read = CreateUtterances(Intents.Read, books, Entities.Book);

            const int maxUtterances = 10000;
            var maxUtterancesPerIntent = maxUtterances / Intents.All.Count;

            simpleCalls = PickRandom(simpleCalls.ToList(), Math.Min(maxUtterancesPerIntent, simpleCalls.Count()));
            advancedCalls = PickRandom(advancedCalls.ToList(), Math.Min(maxUtterancesPerIntent, advancedCalls.Count()));
            message = PickRandom(message.ToList(), Math.Min(maxUtterancesPerIntent, message.Count()));
            watch = PickRandom(watch.ToList(), Math.Min(maxUtterancesPerIntent, watch.Count()));
            read = PickRandom(read.ToList(), Math.Min(maxUtterancesPerIntent, read.Count()));
        
            var utterances = new List<Utterance>();
            utterances.AddRange(simpleCalls);
            utterances.AddRange(advancedCalls);
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

        public static string CreateTextFromPattern(string patternFormat, List<Part> noises, string intent, string entity)
        {
            var noiseMap = Program.noiseMap;
            var stringList = new List<string>();
            foreach (var curNoise in noises)
            {
                if (curNoise == Part.Intent)
                {
                    stringList.Add(intent);
                }
                else if (curNoise == Part.Entity)
                {
                    stringList.Add(entity);
                }
                else
                {
                    var possibleNoises = noiseMap[curNoise];
                    stringList.Add(PickRandom(possibleNoises));
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