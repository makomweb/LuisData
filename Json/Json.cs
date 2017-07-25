using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Serializer
{
    public class Json
    {
        public string Serialize<T>(T model)
        {
            try
            {
                return JsonConvert.SerializeObject(model);
            }
            catch (JsonException ex)
            {
                throw new Exception("Exeption caught while serializing!", ex);
            }
        }

        public void Serialize<T>(Stream destination, T model, bool leaveOpen = false)
        {
            try
            {
                using (var writer = new JsonTextWriter(new StreamWriter(destination, new UTF8Encoding(false), 1024, leaveOpen)))
                {
                    var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto };
                    serializer.Serialize(writer, model);
                }
            }
            catch (JsonException ex)
            {
                throw new Exception("Exeption caught while serializing!", ex);
            }
        }

        public T Deserialize<T>(string json, DefaultValueHandling defaultValueHandling = DefaultValueHandling.Ignore)
        {
            var settings = new JsonSerializerSettings { DefaultValueHandling = defaultValueHandling };
            try
            {
                return JsonConvert.DeserializeObject<T>(json, settings);
            }
            catch (JsonException ex)
            {
                throw new Exception("Exeption caught while deserializing!", ex);
            }
        }

        public T Deserialize<T>(Stream source, DefaultValueHandling defaultValueHandling = DefaultValueHandling.Ignore, bool leaveOpen = false)
        {
            try
            {
                using (var reader = new JsonTextReader(new StreamReader(source, Encoding.UTF8, false, 1024, leaveOpen)))
                {
                    var serializer = new JsonSerializer { TypeNameHandling = TypeNameHandling.Auto, DefaultValueHandling = defaultValueHandling };
                    return serializer.Deserialize<T>(reader);
                }
            }
            catch (JsonException ex)
            {
                throw new Exception("Exeption caught while deserializing!", ex);
            }
        }
    }
}