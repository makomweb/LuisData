using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace GenerateLuisData
{
    [TestClass]
    public class PatternTests
    {
        [TestMethod]
        public async Task RunAsync()
        {
            var text = Program.CreateTextFromPattern(null, null, null, null);
            Assert.IsFalse(string.IsNullOrEmpty(text), "'text' should not be null or empty!");
        }
    }
}
