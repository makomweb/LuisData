using System;
using System.Collections.Generic;

namespace GenerateLuisData
{
    public static class CommandLine
    {
        public static Dictionary<string, List<string>> Parse(string[] args)
        {
            return Parse(args, true, false);
        }

        public static Dictionary<string, List<string>> Parse(string[] args, bool caseInsensitive)
        {
            return Parse(args, caseInsensitive, false);
        }

        public static Dictionary<string, List<string>> Parse(string[] args, bool caseInsensitive, bool multipleKeys)
        {
            var result = new Dictionary<string, List<string>>();

            var currentKey = string.Empty;

            foreach (var argument in args)
            {
                if ((argument.StartsWith("-", StringComparison.OrdinalIgnoreCase) || argument.StartsWith("/", StringComparison.OrdinalIgnoreCase)) && argument.Length > 1)
                {
                    currentKey = argument.Remove(0, 1);
                    if (caseInsensitive)
                    {
                        currentKey = currentKey.ToLowerInvariant();
                    }
                    if (!result.ContainsKey(currentKey))
                    {
                        result.Add(currentKey, null);
                    }
                }
                else
                {
                    List<string> values = null;
                    if (result.ContainsKey(currentKey))
                    {
                        values = result[currentKey];
                    }
                    if (values == null)
                    {
                        values = new List<string>();
                    }
                    values.Add(argument);
                    result[currentKey] = values;
                    if (!multipleKeys)
                    {
                        currentKey = string.Empty;
                    }
                }
            }
            return result;
        }

        public static string Extract(string[] args, string name)
        {
            var res = string.Empty;
            var dict = Parse(args, false, false);
            var list = dict[name];
            foreach (var item in list)
            {
                res = item;
            }
            return res;
        }

        public static string ExtractOrEmpty(string[] args, string name)
        {
            string res;
            try
            {
                res = Extract(args, name);
            }
            catch (Exception /*e*/)
            {
                res = string.Empty;
            }
            return res;
        }
    }
}