using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GZipArchiver.Tests
{
    [TestClass]
    public class SafeDictionaryTests
    {
        [TestMethod]
        public void TestSimpleFlow()
        {
            var dict = new SafeDictionary<string, int> {["a"] = 1};
            Assert.AreEqual(1, dict["a"]);
            Assert.AreEqual(1, dict.Keys.Count());
            int removed;
            Assert.IsTrue(dict.TryRemove("a", out removed));

            Assert.AreEqual(1, removed);
            Assert.AreEqual(0, dict.Keys.Count());
        }
    }
}
