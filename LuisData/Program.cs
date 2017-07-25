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


        private IEnumerable<string> Patterns()
        {
            var patterns = new List<string>();

            //Pattern.Add("{intent} {entity}");
            //Pattern.Add("{intent} {entity} {time}");
            //Pattern.Add("{intent} {entity}");

            return patterns;
        }

        private class IntentSynonyms : Dictionary<string, string> { }

        private static LuisDoc Generate()
        {
            var intentSynonyms = new IntentSynonyms
            {
                { "call", Intents.Call },
                { "dial", Intents.Call },
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

            var utterances = new List<Utterance>();
            utterances.AddRange(call);
            utterances.AddRange(message);
            utterances.AddRange(watch);
            utterances.AddRange(read);
            return utterances;
        }

        private static IEnumerable<Utterance> CreateUtterances(IntentSynonyms synonyms, string intentId, IEnumerable<string> entities, string entityType)
        {
            var pairs = synonyms.Where(pair => pair.Value == intentId);
            foreach (var p in pairs)
            {
                foreach (var e in entities)
                {
                    yield return Utterance.Create(p.Value, p.Key, e, entityType);
                }
            }
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