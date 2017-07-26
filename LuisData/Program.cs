﻿using Serializer;
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
            var names = GetNames();
            var books = GetBooks();
            var movies = GetMovies();

            return Generate(names, movies, books);
        }

        private static LuisDoc Generate(IEnumerable<string> names, IEnumerable<string> movies, IEnumerable<string> books)
        {
            return new LuisDoc()
            {
                luis_schema_version = "2.1.0",
                versionId = "0.2.4",
                culture = "en-us",
                desc = "training data",
                name = "my-radish",
                entities = Entities.All.Select(o => new Entity { name = o }).ToList(),
                intents = Intents.All.Select(o => new Intent { name = o }).ToList(),
                utterances = CreateUtterances(names, movies, books)
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

        private static List<Utterance> CreateUtterances(IEnumerable<string> names, IEnumerable<string> movies, IEnumerable<string> books)
        {
            var call = CreateUtterances(Intents.Call, names, Entities.Contact);
            var message = CreateUtterances(Intents.Message, names, Entities.Contact);
            var watch = CreateUtterances(Intents.Watch, movies, Entities.Movie);
            var read = CreateUtterances(Intents.Read, books, Entities.Book);

            const int maxUtterances = 1000;
            var maxUtterancesPerIntent = maxUtterances / Intents.All.Count;

            call = PickRandom(call.ToList(), Math.Min(maxUtterancesPerIntent, call.Count()));
            message = PickRandom(message.ToList(), Math.Min(maxUtterancesPerIntent, message.Count()));
            watch = PickRandom(watch.ToList(), Math.Min(maxUtterancesPerIntent, watch.Count()));
            read = PickRandom(read.ToList(), Math.Min(maxUtterancesPerIntent, read.Count()));
        
            var utterances = new List<Utterance>();
            utterances.AddRange(call);
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