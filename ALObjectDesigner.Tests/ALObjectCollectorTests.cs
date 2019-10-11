using ALObjectDesigner.Library;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ALObjectDesigner.Tests
{
    public class ALObjectCollectorTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task DiscoverSymbolsTestExplicit()
        {
            var collector = new ALObjectCollector();

            var paths = new List<string>() {
                @".\"
            };

            var symbolsRefs = await collector.Discover(paths);

            Assert.IsTrue(symbolsRefs.Count > 0);
        }

        [Test]
        public async Task DiscoverSymbolsTestImplicit()
        {
            var paths = new List<string>() {
                @".\"
            };

            var collector = new ALObjectCollector(paths);
            var symbolsRefs = await collector.Discover();

            Assert.IsTrue(symbolsRefs.Count > 0);
        }
    }
}