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
            public static string Book = "book";
            public static string Movie = "movie";

            public static List<string> All = new List<string> { Contact, Book, Movie };
        }

        private enum Noise
        {
            Preface,
            Middle,
            Trailer,
            Intent,
            Entity
        }

        private static IDictionary<string, List<Noise>> patterns = new Dictionary<string, List<Noise>>
            {
                { "{intent} {entity}", new List<Noise>() {Noise.Intent, Noise.Entity} },
                { "{entity} {intent}", new List<Noise>() {Noise.Entity, Noise.Intent} },
                { "{intent} {entity} {trailer}", new List<Noise>() { Noise.Intent, Noise.Entity, Noise.Trailer } },
                { "{preface} {intent} {entity}", new List<Noise>() {Noise.Preface, Noise.Intent, Noise.Entity} },
                { "{preface} {intent} {entity} {trailer}", new List<Noise>() { Noise.Preface, Noise.Intent, Noise.Entity, Noise.Trailer } },
                { "{intent} {middle} {entity}", new List<Noise>() { Noise.Intent, Noise.Middle, Noise.Entity } },
                { "{intent} {middle} {entity} {trailer}", new List<Noise>() { Noise.Intent, Noise.Middle, Noise.Entity, Noise.Trailer } },
                { "{preface} {entity} {middle} {intent}", new List<Noise>() { Noise.Preface, Noise.Entity, Noise.Middle, Noise.Intent } },
                { "{preface} {intent} {middle} {entity}", new List<Noise>() { Noise.Preface, Noise.Intent, Noise.Middle, Noise.Entity } },
                { "{preface} {intent} {middle} {entity} {trailer}", new List<Noise>() {Noise.Preface, Noise.Intent, Noise.Middle, Noise.Entity, Noise.Trailer} }
            };


        private static IDictionary<Noise, List<string>> noiseMap = new Dictionary<Noise, List<string>>
                                                                    {
                                                                            { Noise.Preface, new List<string>() { "make", "do", "finish", "set", "complete", "start", "continue" } },
                                                                            { Noise.Middle, new List<string>() { "to", "with", "by", "along", "for" } },
                                                                            { Noise.Trailer, new List<string>() { "again", "tomorrow", "today", "first", "last" } }
                                                                    };

        private class IntentSynonyms : Dictionary<string, string> { }

        private static LuisDoc Generate()
        {
            var intentSynonyms = new IntentSynonyms
            {
                { "call", Intents.Call },
                { "phone", Intents.Call },
                { "email", Intents.Message },
                { "message", Intents.Message },
                { "read", Intents.Read },
                { "research", Intents.Read },
                { "watch", Intents.Watch },
                { "see", Intents.Watch }
            };

            var names = GetNames();
            var books = GetBooks();
            var movies = GetMovies();

            return Generate(intentSynonyms, names, movies, books);
        }

        private static LuisDoc Generate(IntentSynonyms synonyms, IEnumerable<string> names, IEnumerable<string> movies, IEnumerable<string> books)
        {
            return new LuisDoc()
            {
                luis_schema_version = "2.1.0",
                versionId = "0.1.2",
                culture = "en-us",
                desc = "training data",
                name = "my-radish",
                entities = Entities.All.Select(o => new Entity { name = o }).ToList(),
                intents = Intents.All.Select(o => new Intent { name = o }).ToList(),
                utterances = CreateUtterances(synonyms, names, movies, books)
            };
        }

        private static IEnumerable<string> GetNames()
        {
            var names = GetLines(@"../../names.dat").ToList();
            names.AddRange(GetLines(@"../../advanced-names.dat"));
            return names;
        }

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

        private static List<Utterance> CreateUtterances(IntentSynonyms synonyms, IEnumerable<string> names, IEnumerable<string> movies, IEnumerable<string> books)
        {
            var call = CreateUtterances(synonyms, Intents.Call, names, Entities.Contact);
            var message = CreateUtterances(synonyms, Intents.Message, names, Entities.Contact);
            var watch = CreateUtterances(synonyms, Intents.Watch, movies, Entities.Movie);
            var read = CreateUtterances(synonyms, Intents.Read, books, Entities.Book);

            var numUtterances = 10000 / Intents.All.Count;
            call = pickRandom(call.ToList(), Math.Min(numUtterances, call.Count()));
            message = pickRandom(message.ToList(), Math.Min(numUtterances, message.Count()));
            watch = pickRandom(watch.ToList(), Math.Min(numUtterances, watch.Count()));
            read = pickRandom(read.ToList(), Math.Min(numUtterances, read.Count()));
        
            var utterances = new List<Utterance>();
            utterances.AddRange(call);
            utterances.AddRange(message);
            utterances.AddRange(watch);
            utterances.AddRange(read);
            return utterances;
        }

        private static IEnumerable<Utterance> CreateUtterances(IntentSynonyms synonyms, string intentId, IEnumerable<string> entities, string entityType)
        {
            //var temp = string.Format("{0} {1}", new string[] { "hi", "hello" });
            var pairs = synonyms.Where(pair => pair.Value == intentId);
            foreach (var p in pairs)
            {
                foreach (var e in entities)
                {
                    foreach (var pattern in patterns.Keys)
                    {
                        var text = CreateTextFromPattern(pattern, patterns[pattern], p.Key, e);
                        yield return Utterance.Create(text, p.Value, p.Key, e, entityType);
                    }
                }
            }
        }//TODO: spoof the intent name a little bit on some

        private static string CreateTextFromPattern(string patternFormat, List<Noise> noises, string intent, string entity)
        {
            var noiseMap = Program.noiseMap;
            var stringList = new List<string>();
            foreach (var curNoise in noises)
            {
                if (curNoise == Noise.Intent)
                {
                    stringList.Add(intent);
                }
                else if (curNoise == Noise.Entity)
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