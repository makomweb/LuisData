﻿using Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GenerateLuisData
{
    public class Program
    {
        static void Main(string[] args)
        {
            var doc = Generate(_intentSynonyms);
            var file = OpenFile(doc);
            new Json().Serialize(file, doc);
        }

        private static FileStream OpenFile(LuisDoc doc)
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var fileName = $"luis-training-data-v{doc.versionId}.json";
            var path = Path.Combine(folder, fileName);
            return File.OpenWrite(path);
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

        private class IntentSynonyms : Dictionary<string, string> { }

        private class EntityExamples : Dictionary<string, string> { }

        private static IntentSynonyms _intentSynonyms = new IntentSynonyms();

        private static List<string> _patterns = new List<string>();

        static Program()
        {
            _intentSynonyms.Add("call", Intents.Call);
            _intentSynonyms.Add("email", Intents.Message);
            _intentSynonyms.Add("message", Intents.Message);
            _intentSynonyms.Add("dial", Intents.Call);
            _intentSynonyms.Add("read", Intents.Read);
            _intentSynonyms.Add("research", Intents.Read);
            _intentSynonyms.Add("watch", Intents.Watch);

            //Pattern.Add("{intent} {entity}");
            //Pattern.Add("{intent} {entity} {time}");
            //Pattern.Add("{intent} {entity}");
        }

        private static LuisDoc Generate(IntentSynonyms synonyms)
        {
            var names = GetNames();
            var books = GetBooks();
            var movies = GetMovies();
            var generator = new Generator(_intentSynonyms, names, movies, books);
            return generator.Create();
        }

        private class Generator
        {
            private readonly IntentSynonyms _synonyms;
            private readonly IEnumerable<string> _names;
            private readonly IEnumerable<string> _movies;
            private readonly IEnumerable<string> _books;

            public Generator(IntentSynonyms synonyms, IEnumerable<string> names, IEnumerable<string> movies, IEnumerable<string> books)
            {
                _synonyms = synonyms;
                _names = names;
                _movies = movies;
                _books = books;
            }

            private IEnumerable<Utterance> CreateUtterances(string intentId, IEnumerable<string> entities, string entityType)
            {
                var pairs = _synonyms.Where(pair => pair.Value == intentId);
                foreach (var p in pairs)
                {
                    foreach (var e in entities)
                    {
                        yield return Utterance.Create(p.Value, p.Key, e, entityType);
                    }
                }
            }

            public LuisDoc Create()
            {
                var callUtterances = CreateUtterances(Intents.Call, _names, Entities.Contact);
                var messageUtterances = CreateUtterances(Intents.Message, _names, Entities.Contact);
                var watchUtterances = CreateUtterances(Intents.Watch, _movies, Entities.Movie);
                var readUtterances = CreateUtterances(Intents.Read, _books, Entities.Book);

                var utterances = new List<Utterance>();
                utterances.AddRange(callUtterances);
                utterances.AddRange(messageUtterances);
                utterances.AddRange(watchUtterances);
                utterances.AddRange(readUtterances);

                return new LuisDoc()
                {
                    luis_schema_version = "2.1.0",
                    versionId = "0.1.2",
                    culture = "en-us",
                    desc = "training data",
                    name = "my-radish",
                    entities = Entities.All.Select(o => new Entity { name = o }).ToList(),
                    intents = Intents.All.Select(o => new Intent { name = o }).ToList(),
                    utterances = utterances
                };
            }
        }
    }
}