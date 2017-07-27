using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace GenerateLuisData
{
    public partial class Program
    {
        public class PartOptionCombination
        {
            private PartOptionCombination(string preface, string middle, string trailer)                
            {
                Preface = preface;
                Middle = middle;
                Trailer = trailer;
            }

            public string Preface { get; private set; }
            public string Middle { get; private set; }
            public string Trailer { get; private set; }

            public static IEnumerable<PartOptionCombination> FromParts(IEnumerable<Part> parts)
            {
                Debug.Assert(parts.Contains(Part.Intent), "'parts' must contain and intent!");
                Debug.Assert(parts.Contains(Part.Entity), "'parts' must contain and entity!");

                var result = new List<PartOptionCombination>();

                var containsPreface = parts.Contains(Part.Preface);
                var containsMiddle = parts.Contains(Part.Middle);
                var containsTrailer = parts.Contains(Part.Trailer);

                return CreateCombinations(containsPreface, containsMiddle, containsTrailer);
            }

            private static IEnumerable<PartOptionCombination> CreateCombinations(bool containsPreface, bool containsMiddle, bool containsTrailer)
            {
                if (containsPreface && !containsMiddle && !containsTrailer)
                {
                    foreach (var p in partOptions[Part.Preface])
                    {
                        yield return new PartOptionCombination(p, string.Empty, string.Empty);
                    }
                }

                if (!containsPreface && containsMiddle && !containsTrailer)
                {
                    foreach (var m in partOptions[Part.Middle])
                    {
                        yield return new PartOptionCombination(string.Empty, m, string.Empty);
                    }
                }

                if (!containsPreface && !containsMiddle && containsTrailer)
                {
                    foreach (var t in partOptions[Part.Trailer])
                    {
                        yield return new PartOptionCombination(string.Empty, string.Empty, t);
                    }
                }

                if (containsPreface && containsMiddle && !containsTrailer)
                {
                    foreach (var p in partOptions[Part.Preface])
                    {
                        foreach (var m in partOptions[Part.Middle])
                        {
                            yield return new PartOptionCombination(p, m, string.Empty);
                        }
                    }
                }

                if (containsPreface && !containsMiddle && containsTrailer)
                {
                    foreach (var p in partOptions[Part.Preface])
                    {
                        foreach (var t in partOptions[Part.Trailer])
                        {
                            yield return new PartOptionCombination(p, string.Empty, t);
                        }
                    }
                }

                if (!containsPreface && containsMiddle && containsTrailer)
                {
                    foreach (var m in partOptions[Part.Middle])
                    {
                        foreach (var t in partOptions[Part.Trailer])
                        {
                            yield return new PartOptionCombination(string.Empty, m, t);
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
                                yield return new PartOptionCombination(p, m, t);
                            }
                        }
                    }
                }
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

            [Conditional("DEBUG")]
            private void Assert(IEnumerable<Part> pattern)
            {
                Debug.Assert(pattern.Contains(Part.Preface) == !string.IsNullOrEmpty(Preface), "Can't provide 'Preface'!");
                Debug.Assert(pattern.Contains(Part.Middle) == !string.IsNullOrEmpty(Middle), "Can't provide 'Middle'!");
                Debug.Assert(pattern.Contains(Part.Trailer) == !string.IsNullOrEmpty(Trailer), "Can't provide 'Trailer'!");
            }
        }        
    }
}