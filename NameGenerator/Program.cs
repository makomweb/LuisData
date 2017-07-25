using Serializer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace NameGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Name> names = new List<Name>();
            const int MAX = 200;
            for (var i = 0; i < MAX; ++i)
            {
                var n = FetchNameAsync().Result;
                names.Add(n);
                Console.Write(".");
            }

            var file = OpenFile();
            using (var writer = new StreamWriter(file))
            {
                foreach (var n in names)
                {
                    writer.WriteLine($"{n.First} {n.Last}");
                }
            }

            Console.ReadKey();
        }
        
        private static FileStream OpenFile()
        {
            var folder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var fileName = $"advanced-names.dat";
            var path = Path.Combine(folder, fileName);
            return File.OpenWrite(path);
        }

        private class Name
        {
            private dynamic _name;

            public Name(dynamic obj)
            {
                var props = System.Linq.Enumerable.First(obj.results);
                _name = props["name"];
            }

            public string Title => _name.title;
            public string First => _name.first;
            public string Last => _name.last;
        }

        private static async Task<Name> FetchNameAsync()
        {
            var http = new HttpClient();
            var response = await http.GetAsync("https://randomuser.me/api/");
            Assert(response);
            var body = await response.Content.ReadAsStringAsync();
            var json = new Json();
            var obj = json.Deserialize<dynamic>(body);
            return new Name(obj);
        }

        private static void Assert(HttpResponseMessage message)
        {
            if (!message.IsSuccessStatusCode)
            {
                throw new Exception("HTTP request failed!");
            }
        }
    }
}
