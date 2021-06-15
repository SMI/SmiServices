using IsIdentifiableReviewer;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terminal.Gui;
using YamlDotNet.Serialization;

namespace Microservices.IsIdentifiable.Tests
{
    class TerminalGuiThemeTests
    {
        [Test]
        public void TestDeserialization()
        {
            var themeFile = Path.Combine(TestContext.CurrentContext.TestDirectory, "theme.yaml");
            var des = new Deserializer();

            var theme = des.Deserialize<TerminalGuiTheme>(File.ReadAllText(themeFile));

            Assert.AreNotEqual(default(Color), theme.HotFocusBackground);
            Assert.AreNotEqual(default(Color), theme.HotFocusForeground);
            Assert.AreEqual(Color.Black, theme.FocusForeground);
            Assert.AreNotEqual(default(Color), theme.FocusBackground);
            Assert.AreNotEqual(default(Color), theme.HotNormalBackground);
            Assert.AreNotEqual(default(Color), theme.HotNormalForeground);

            theme = new TerminalGuiTheme();

            Assert.AreEqual(default(Color), theme.HotFocusBackground);
            Assert.AreEqual(default(Color), theme.HotFocusForeground);
            Assert.AreEqual(default(Color), theme.FocusForeground);
            Assert.AreEqual(default(Color), theme.FocusBackground);
            Assert.AreEqual(default(Color), theme.HotNormalBackground);
            Assert.AreEqual(default(Color), theme.HotNormalForeground);

        }
    }
}
