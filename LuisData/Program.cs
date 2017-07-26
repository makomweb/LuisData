using Serializer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                { "{intent} {entity} {trailer}", new List<Part>() { Part.Intent, Part.Entity, Part.Trailer } },
                { "{preface} {intent} {entity}", new List<Part>() {Part.Preface, Part.Intent, Part.Entity} },
                { "{preface} {intent} {entity} {trailer}", new List<Part>() { Part.Preface, Part.Intent, Part.Entity, Part.Trailer } },
                { "{intent} {middle} {entity}", new List<Part>() { Part.Intent, Part.Middle, Part.Entity } },
                { "{intent} {middle} {entity} {trailer}", new List<Part>() { Part.Intent, Part.Middle, Part.Entity, Part.Trailer } },
                { "{preface} {intent} {middle} {entity}", new List<Part>() { Part.Preface, Part.Intent, Part.Middle, Part.Entity } },
                { "{preface} {intent} {middle} {entity} {trailer}", new List<Part>() {Part.Preface, Part.Intent, Part.Middle, Part.Entity, Part.Trailer} }
            };

        private static IDictionary<Part, List<string>> partOptions = new Dictionary<Part, List<string>>
                                                                    {
                                                                            { Part.Preface, new List<string>() { "make", "do", "finish", "set", "complete", "start", "continue" } },
                                                                            { Part.Middle, new List<string>() { "to", "with", "by", "along", "for" } },
                                                                            { Part.Trailer, new List<string>() { "again", "tomorrow", "today", "first", "last", "later" } }
                                                                    };

        public class PartOptionCombination
        {
            public PartOptionCombination(string preface, string middle, string trailer)                
            {
                Preface = preface;
                Middle = middle;
                Trailer = trailer;
            }

            public string Preface { get; private set; }
            public string Middle { get; private set; }
            public string Trailer { get; private set; }

            [Conditional("DEBUG")]
            private void Assert(IEnumerable<Part> pattern)
            {
                Debug.Assert(pattern.Contains(Part.Preface) == !string.IsNullOrEmpty(Preface), "Can't provide 'Preface'!");
                Debug.Assert(pattern.Contains(Part.Middle) == !string.IsNullOrEmpty(Middle), "Can't provide 'Middle'!");
                Debug.Assert(pattern.Contains(Part.Trailer) == !string.IsNullOrEmpty(Trailer), "Can't provide 'Trailer'!");
            }

            public string CreateText(IEnumerable<Part> pattern, string intent, string entity)
            {
                Assert(pattern);

                var result = new List<PartOptionCombination>();

                var containsPreface = pattern.Contains(Part.Preface);
                var containsMiddle = pattern.Contains(Part.Middle);
                var containsTrailer = pattern.Contains(Part.Trailer);

                if (containsPreface)
                {
                    if (containsMiddle)
                    {
                        if (containsTrailer)
                        {
                            return $"{Preface} {intent} {Middle} {entity} {Trailer}";
                        }
                        else
                        {
                            return $"{Preface} {intent} {Middle} {entity}";
                        }
                    }
                    else
                    {
                        if (containsTrailer)
                        {
                            return $"{Preface} {intent} {entity} {Trailer}";
                        }
                        else
                        {
                            return $"{Preface} {intent} {entity}";
                        }
                    }
                }
                else
                {
                    if (containsMiddle)
                    {
                        if (containsTrailer)
                        {
                            return $"{intent} {Middle} {entity} {Trailer}";
                        }
                        else
                        {
                            return $"{intent} {Middle} {entity}";
                        }
                    }
                    else
                    {
                        if (containsTrailer)
                        {
                            return $"{intent} {entity} {Trailer}";
                        }
                        else
                        {
                            return $"{intent} {entity}";
                        }
                    }
                }
            }
        }

        public static IEnumerable<PartOptionCombination> CreateCombinations(IEnumerable<Part> parts)
        {
            Debug.Assert(parts.Contains(Part.Intent), "'parts' must contain and intent!");
            Debug.Assert(parts.Contains(Part.Entity), "'parts' must contain and entity!");

            var result = new List<PartOptionCombination>();

            var containsPreface = parts.Contains(Part.Preface);
            var containsMiddle = parts.Contains(Part.Middle);
            var containsTrailer = parts.Contains(Part.Trailer);

            if (containsPreface && !containsMiddle && !containsTrailer)
            {
                foreach (var p in partOptions[Part.Preface])
                {
                    result.Add(new PartOptionCombination(p, string.Empty, string.Empty));
                }
            }

            if (!containsPreface && containsMiddle && !containsTrailer)
            {
                foreach (var m in partOptions[Part.Middle])
                {
                    result.Add(new PartOptionCombination(string.Empty, m, string.Empty));
                }
            }

            if (!containsPreface && !containsMiddle && containsTrailer)
            {
                foreach (var t in partOptions[Part.Trailer])
                {
                    result.Add(new PartOptionCombination(string.Empty, string.Empty, t));
                }
            }

            if (containsPreface && containsMiddle && !containsTrailer)
            {
                foreach (var p in partOptions[Part.Preface])
                {
                    foreach (var m in partOptions[Part.Middle])
                    {
                        result.Add(new PartOptionCombination(p, m, string.Empty));
                    }
                }
            }

            if (containsPreface && !containsMiddle && containsTrailer)
            {
                foreach (var p in partOptions[Part.Preface])
                {
                    foreach (var t in partOptions[Part.Trailer])
                    {
                        result.Add(new PartOptionCombination(p, string.Empty, t));
                    }
                }
            }

            if (!containsPreface && containsMiddle && containsTrailer)
            {
                foreach (var m in partOptions[Part.Middle])
                {
                    foreach (var t in partOptions[Part.Trailer])
                    {
                        result.Add(new PartOptionCombination(string.Empty, m, t));
                    }
                }
            }

            if (containsPreface && containsMiddle && containsTrailer)
            {
                foreach (var p in partOptions[Part.Preface])
                {
                    foreach (var m in partOptions[Part.Middle])
                    {
                        foreach (var t in partOptions[Part.Trailer])
                        {
                            result.Add(new PartOptionCombination(p, m, t));
                        }
                    }
                }
            }

            return result;
        }

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
                versionId = "0.2.12",
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
            var result = new List<Utterance>();
            foreach (var entity in entities)
            {
                foreach (var pattern in patterns.Keys)
                {
#if false
                    var text = CreateTextFromPattern(pattern, patterns[pattern], intent, entity);
                    yield return Utterance.Create(text, intent, entity, entityType);
#else
                    var texts = CreateTextsFromPattern(pattern, patterns[pattern], intent, entity);

                    foreach (var t in texts)
                    {
                        result.Add(Utterance.Create(t, intent, entity, entityType));
                    }
#endif
                }
            }
            return result;
        }

        //TODO: spoof the intent name a little bit on some

        public static IEnumerable<string> CreateTextsFromPattern(string patternFormat, List<Part> parts, string intent, string entity)
        {
            var result = new List<string>();
            var combinations = CreateCombinations(parts);
            if (combinations.Any())
            {
                foreach (var combination in combinations)
                {
                    result.Add(combination.CreateText(parts, intent, entity));
                }
            }
            else
            {
                result.Add($"{intent} {entity}");
            }

            return result;
        }

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