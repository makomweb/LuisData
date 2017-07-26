using System.Threading.Tasks;
using Xunit;

namespace Tests
{
    public class PatternTests
    {
        [Theory]
        public async Task RunAsync()
        {
            var text = GenerateLuisData.Program.CreateTextFromPattern(null, null, null, null);
            Assert.False(string.IsNullOrEmpty(text), "'text' should not be null or empty!");
        }
    }
}
