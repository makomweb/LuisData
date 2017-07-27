using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Diagnostics;

namespace GenerateLuisData
{
    [TestClass]
    public class PatternTests
    {
        [TestMethod]
        public void Run()
        {
            var parts = new List<Program.Part>() { Program.Part.Preface, Program.Part.Intent, Program.Part.Middle, Program.Part.Entity, Program.Part.Trailer };
            var combinations = Program.PartOptionCombination.FromParts(parts);
            Assert.IsNotNull(combinations);
            foreach (var c in combinations)
            {
                Debug.WriteLine(AsHumanReadible(c));
            }
        }

        private static string AsHumanReadible(Program.PartOptionCombination combination)
        {
            return $"{combination.Preface} - {combination.Middle} - {combination.Trailer}";
        }
    }
}
